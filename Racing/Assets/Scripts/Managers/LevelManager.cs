using System;
using System.Collections;
using System.Collections.Generic;
using DistantLands.Cozy;
using DistantLands.Cozy.Data;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum Weather
{
    Clear,
    Rainy,
    Snowy
}

public enum DayTime
{
    Morning,
    Noon,
    Evening,
    Night
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
    Expert,
    Unbeatable
}

public class LevelManager : MonoBehaviour
{
    [Header("Stage Settings")]
    [SerializeField] public GameObject pickedCar;
    [SerializeField] private int pickedCarColor;
    [SerializeField] private Weather weather;
    [SerializeField] private DayTime dayTime;
    [SerializeField] public RaceMode raceMode;
    [SerializeField] public Difficulty difficulty;
    [SerializeField] public bool reverse;
    [SerializeField] public int laps = 3;
    [SerializeField] public int bots = 4;

    [SerializeField] public bool botCar = false;

    [Header("Surface Grip")] 
    [SerializeField] public float trackGrip = 1f;
    [SerializeField] public float otherGrip = 0.7f;
    
    [Header("Graphics")] 
    [SerializeField] private GameObject grass;

    [Header("Technical")] 
    [SerializeField] private GameObject driftUI;
    [SerializeField] private GameObject timeAttackUI;
    [SerializeField] private GameObject raceUI;
    [SerializeField] private GameObject mobileUI;
    [SerializeField] private GameObject wrongDirectionSign;
    [SerializeField] private GameObject directionArrow;
    [SerializeField] private GameObject resetCarUI;
    [SerializeField] private GameObject challengeResults;
    [SerializeField] private GameObject challengeInformation;
    
    [SerializeField] private Transform activeCarMarker;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<int> dayTimes;
    [SerializeField] private List<WeatherProfile> weathers;
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Sprite starEmpty;

    [SerializeField] public bool nightMode = false;

    private Transform _startPosition;
    private List<Car> _cars = new();

    public List<SpawnPos> botSpawns = new();

    private int _activeCar = 0;
    public int currentLap = 1;

    public float playerDistanceLimit;
    
    public bool wrongDirection = false;
    public bool wrongDirectionActive = false;
    public bool resetCar = false;
    public bool carStarted = false;
    private bool _playerFinished = false;

    public CheckPoint lastCheckPoint;
    public CarPlayer player;

    public event Action OnLapFinish;
    public event Action OnStageFinish;
    public event Action OnCheckpoint;

    private AudioSource _audioSource;
    private Controls _controls;
    private CozyWeather _cozyWeather;
    private ReflectionProbe _reflectionProbe;

    private void Awake()
    {
        if (GameManager.Get())
        {
            GameManager gameManager = GameManager.Get();
            pickedCar = gameManager.car;
            pickedCarColor = gameManager.carColorId;
            weather = gameManager.weather;
            dayTime = gameManager.dayTime;
            raceMode = gameManager.raceMode;
            difficulty = gameManager.difficulty;
            reverse = gameManager.stageReverse;
            laps = gameManager.laps;
            bots = gameManager.bots;

            if (gameManager.graphicsQuality == QualityLevel.High && grass)
            {
                grass.SetActive(true);
            }

            botCar = false;
        }
        
        _controls = Controls.Get();
        _cozyWeather = FindObjectOfType<CozyWeather>();
        _audioSource = GetComponent<AudioSource>();
        _reflectionProbe = FindObjectOfType<ReflectionProbe>();

        if (weather is Weather.Rainy or Weather.Snowy) nightMode = true;
        if (dayTime == DayTime.Night) nightMode = true;

        Application.targetFrameRate = 60;

        switch (raceMode)
        {
            case RaceMode.TimeAttack:
                timeAttackUI.SetActive(true);
                break;
            case RaceMode.Drift:
                driftUI.SetActive(true);
                break;
            case RaceMode.Race:
                raceUI.SetActive(true);
                break;
        }

        if (Application.isMobilePlatform)
        {
            mobileUI.SetActive(true);
        }
        
        UpdateWeather();
        StartCoroutine(UpdateReflectionProbe(0.1f));
    }

    private void Start()
    {
        _startPosition = GameObject.FindWithTag("Respawn").transform;

        if (reverse) _startPosition.forward = -_startPosition.forward;
        if (raceMode == RaceMode.Race) InitStartingGrid();
        
        Ray ray = new()
        {
            origin = _startPosition.position + Vector3.up * 10f,
            direction = Vector3.down
        };
        
        Physics.Raycast(ray, out RaycastHit raycastHit,  100f, (1 << 7) | (1 << 0), QueryTriggerInteraction.Ignore);
        
        GameObject playerCar = Instantiate(pickedCar, raycastHit.point, _startPosition.rotation);
        _cars.Add(playerCar.GetComponent<Car>());
        
        if (GameManager.Get())
        {
            _cars[_activeCar].SetColor(GameManager.Get().carColors[pickedCarColor]);
            
            if (GameManager.Get().challengeManager)
            {
                StartCoroutine(UpdateChallengeInformation());
            }
        }
        
        activeCarMarker.position = playerCar.transform.position + playerCar.transform.up;
        cameraTarget.position = playerCar.transform.position;
        cameraTarget.rotation = playerCar.transform.rotation;
        
        foreach (Car car in _cars)
        {
            Destroy(car.GetComponent<CarPlayer>());
        }
        UpdateTargetCar();
    }

    private void InitStartingGrid()
    {
        RacingLine racingLine = FindFirstObjectByType<RacingLine>();
        
        _startPosition.position -= _startPosition.right * 2f;
        
        Transform startPos = _startPosition;
        
        int sign = 1;
        Vector3 offset = Vector3.zero;

        Vector3 lineDirection = startPos.forward;
        
        for (int i = 0; i < bots; i++)
        {
            int nearestNode = racingLine.GetNearestNodeID(startPos.position + offset);
            int prevNode = racingLine.ForecastRacingNode(nearestNode, -1);
            lineDirection = racingLine.orderedNodes[prevNode].position -
                                    racingLine.orderedNodes[nearestNode].position;
            
            botSpawns.Add(new SpawnPos(startPos.position + offset, -lineDirection.normalized));

            offset += startPos.right * 4f * sign;
            offset += lineDirection.normalized * 6f;
            sign = -sign;
        }
        
        startPos.position += offset;
        startPos.rotation = Quaternion.LookRotation(-lineDirection.normalized, Vector3.up);
    }

    private void UpdateWeather()
    {
        int time = dayTimes[(int)dayTime];
        if (weather is Weather.Rainy or Weather.Snowy && dayTime == DayTime.Night) time += 1;
        _cozyWeather.timeModule.currentTime = new MeridiemTime(time, 0);
        
        CozyWeather.instance.weatherModule.ecosystem.SetWeather(weathers[(int) weather], 0f);
    }

    private IEnumerator UpdateReflectionProbe(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        _reflectionProbe.RenderProbe();
    }

    private void Update()
    {
        HandleInput();
        HandleCarMarkers();
    }

    private void HandleInput()
    {
        if (_controls.GetKeyDown(ControlKey.CycleCar))
        {
            Destroy(_cars[_activeCar].GetComponent<CarPlayer>());
            _activeCar += 1;
            if (_activeCar >= _cars.Count) _activeCar = 0;
            UpdateTargetCar();
        }
    }

    private float _lerpSpeed = 0f;
    private void HandleCarMarkers()
    {
        Car activeCar = _cars[_activeCar];
        
        activeCarMarker.position = activeCar.transform.position + activeCar.transform.up;

        cameraTarget.position = activeCarMarker.position;
        if (_cars[_activeCar].wheelContact)
        {
            _lerpSpeed = Mathf.Clamp01(_lerpSpeed + Time.deltaTime);

            Vector3 targetRotation = Quaternion.LookRotation(activeCar.rbVelocity).eulerAngles;
            if (_cars[_activeCar].rbVelocity.magnitude < 2f)
            {
                targetRotation = activeCar.transform.rotation.eulerAngles;
            }
            else
            {
                targetRotation.y =activeCar.transform.rotation.eulerAngles.y;
            }
            
            cameraTarget.rotation = Quaternion.Lerp(cameraTarget.rotation,
                Quaternion.Euler(targetRotation), _lerpSpeed * Time.deltaTime * 10f);
        }
        else
        {
            _lerpSpeed = 0f;
        }
    }

    private void UpdateTargetCar()
    {
        if (!_cars[_activeCar].GetComponent<CarPlayer>() && !botCar)
        {
            _cars[_activeCar].AddComponent<CarPlayer>();
        }
        
        if (!_cars[_activeCar].GetComponent<CarBot>() && botCar)
        {
            _cars[_activeCar].AddComponent<CarBot>();
        }
    }

    public Car GetActiveCar()
    {
        return _cars[_activeCar];
    }

    public void WrongDirection(bool state)
    {
        if (_playerFinished) return;
        
        wrongDirectionSign.SetActive(state);
        directionArrow.SetActive(state);

        directionArrow.GetComponent<DirectionArrow>().SetTarget(lastCheckPoint.GetNext().transform);
        
        wrongDirectionActive = state;
    }

    public void ResetCar(bool state)
    {
        if (_playerFinished) return;
        if (resetCar == state) return;
        
        resetCarUI.SetActive(state);
        resetCar = state;
    }

    public void CheckpointReached(CheckPoint checkpoint)
    {
        lastCheckPoint = checkpoint;
        _audioSource.pitch = Random.Range(1.1f, 1.2f);
        _audioSource.Play();
        
        if (checkpoint.isStart) LapFinished();
        
        OnCheckpoint?.Invoke();
    }

    public void LapFinished()
    {
        currentLap += 1;
        
        OnLapFinish?.Invoke();

        if (currentLap > laps)
        {
            OnPlayerFinish();
        }
    }

    public void OnCarStarted()
    {
        carStarted = true;
    }

    public void DisableControls(bool stopCar)
    {
        mobileUI.SetActive(false);

        if (stopCar)
        {
            _cars[_activeCar].StopCar();
        }
    }

    public void OnPlayerFinish()
    {
        OnStageFinish?.Invoke();
        
        DisableControls(true);
        resetCarUI.SetActive(false);
        WrongDirection(false);

        _playerFinished = true;
        
        if (GameManager.Get().challengeManager) CheckChallengeCompletion();
    }

    private IEnumerator UpdateChallengeInformation()
    {
        challengeInformation.SetActive(true);

        List<ChallengeRequirement> challenges = GameManager.Get().challengeManager.challenges;

        List<Image> stars = new();

        for (int i = 0; i < challenges.Count; i++)
        {
            Transform challenge = challengeInformation.transform.GetChild(i);
            challenge.GetComponentInChildren<TMP_Text>().text = challenges[i].caption;
            stars.Add(challenge.GetComponentInChildren<Image>());
        }
        
        yield return null;
        
        while (!_playerFinished)
        {
            for (int i = 0; i < challenges.Count; i++)
            {
                stars[i].sprite = challenges[i].GetCompletionResult() ? starFilled : starEmpty;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void CheckChallengeCompletion()
    {
        challengeResults.SetActive(true);
        challengeInformation.SetActive(false);
        
        ProcessChallenge(0);
        ProcessChallenge(1);
        ProcessChallenge(2);
    }

    private void ProcessChallenge(int id)
    {
        ChallengeManager challengeManager = GameManager.Get().challengeManager;
        ChallengeRequirement requirement = challengeManager.challenges[id];
        
        Transform challenge = challengeResults.transform.GetChild(id);
        challenge.GetComponentInChildren<TMP_Text>().text = requirement.caption;

        if (requirement.GetCompletionResult())
        {
            challenge.GetComponentInChildren<Image>().sprite = starFilled;
            GameManager.Get().challengeData[challengeManager.id] =
                GameManager.Get().challengeData[challengeManager.id] | (1 << id);
        }
    }
}
