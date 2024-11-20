using System;
using System.Collections;
using System.Collections.Generic;
using DistantLands.Cozy;
using DistantLands.Cozy.Data;
using TMPro;
using Unity.Cinemachine;
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
    [SerializeField] private GameObject starParticles;

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

    private CinemachineVirtualCamera _cinemachineCamera;

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

            ChallengeManager challengeManager = GameManager.Get().challengeManager;
            if (challengeManager)
            {
                if (challengeManager.mapExpansion)
                {
                    Instantiate(challengeManager.mapExpansion);
                }
            }
        }
        
        _controls = Controls.Get();
        _cozyWeather = FindFirstObjectByType<CozyWeather>();
        _audioSource = GetComponent<AudioSource>();
        _reflectionProbe = FindFirstObjectByType<ReflectionProbe>();
        _cinemachineCamera = GameObject.FindWithTag("Main VCam").GetComponent<CinemachineVirtualCamera>();

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
            default:
                throw new ArgumentOutOfRangeException();
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
        
        Physics.Raycast(ray, out RaycastHit raycastHit,  100f, (1 << 7) | (1 << 0) | (1 << 10), QueryTriggerInteraction.Ignore);
        
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

        int lastNearestNode = -1;
        for (int i = 0; i < bots; i++)
        {
            int nearestNode = racingLine.GetNearestNodeID(startPos.position + offset);
            int diff = Mathf.Abs(lastNearestNode - nearestNode);
            if (diff > racingLine.orderedNodes.Count / 2) diff -= racingLine.orderedNodes.Count;
            if (lastNearestNode != -1 && Mathf.Abs(diff) > 10)
            {
                nearestNode = lastNearestNode;
            }
            int prevNode = racingLine.ForecastRacingNode(nearestNode, -1);
            lineDirection = racingLine.orderedNodes[prevNode].position -
                                    racingLine.orderedNodes[nearestNode].position;
            
            botSpawns.Add(new SpawnPos(startPos.position + offset, -lineDirection.normalized));

            offset += startPos.right * 4f * sign;
            offset += lineDirection.normalized * 6f;
            sign = -sign;

            lastNearestNode = nearestNode;
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
        
        if (GameManager.Get()?.challengeManager) CheckChallengeCompletion();
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
        
        StartCoroutine(ProcessChallenge(0, 0.5f));
        StartCoroutine(ProcessChallenge(1, 1f));
        StartCoroutine(ProcessChallenge(2, 1.5f));
    }

    private IEnumerator ProcessChallenge(int id, float starDelay)
    {
        ChallengeManager challengeManager = GameManager.Get().challengeManager;
        ChallengeRequirement requirement = challengeManager.challenges[id];
        
        Transform challenge = challengeResults.transform.GetChild(id);
        challenge.GetComponentInChildren<TMP_Text>().text = requirement.caption;

        if (requirement.GetCompletionResult())
        {
            GameManager.Get().challengeData[challengeManager.id] =
                GameManager.Get().challengeData[challengeManager.id] | (1 << id);

            yield return new WaitForSeconds(starDelay);
            
            Image star = challenge.GetComponentInChildren<Image>();
            star.sprite = starFilled;

            GameObject particles = Instantiate(starParticles, star.transform);
            particles.transform.localScale /= star.transform.localScale.x;
            
            Destroy(particles, 2f);
        }
    }
    
    private IEnumerator SnapCameraToTarget()
    {
        cameraTarget.rotation = player.transform.rotation;
        
        CinemachineTransposer transposer = _cinemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
        CinemachineFramingTransposer framingTransposer = _cinemachineCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        CinemachineComposer composer = _cinemachineCamera.GetCinemachineComponent<CinemachineComposer>();
        
        float originalDamping = transposer ? transposer.m_XDamping : 0;
        float originalYDamping = transposer ? transposer.m_YDamping : 0;
        float originalZDamping = transposer ? transposer.m_ZDamping : 0;

        float originalFramingXDamping = framingTransposer ? framingTransposer.m_XDamping : 0;
        float originalFramingYDamping = framingTransposer ? framingTransposer.m_YDamping : 0;
        float originalFramingZDamping = framingTransposer ? framingTransposer.m_ZDamping : 0;

        float originalHorizontalDampening = composer ? composer.m_HorizontalDamping : 0;
        float originalVerticalDampening = composer ? composer.m_VerticalDamping : 0;
        
        if (transposer)
        {
            transposer.m_XDamping = 0;
            transposer.m_YDamping = 0;
            transposer.m_ZDamping = 0;
        }

        if (framingTransposer)
        {
            framingTransposer.m_XDamping = 0;
            framingTransposer.m_YDamping = 0;
            framingTransposer.m_ZDamping = 0;
        }

        if (composer)
        {
            composer.m_HorizontalDamping = 0;
            composer.m_VerticalDamping = 0;
        }
        
        yield return null;
        
        if (transposer)
        {
            transposer.m_XDamping = originalDamping;
            transposer.m_YDamping = originalYDamping;
            transposer.m_ZDamping = originalZDamping;
        }

        if (framingTransposer)
        {
            framingTransposer.m_XDamping = originalFramingXDamping;
            framingTransposer.m_YDamping = originalFramingYDamping;
            framingTransposer.m_ZDamping = originalFramingZDamping;
        }
        
        if (composer)
        {
            composer.m_HorizontalDamping = originalHorizontalDampening;
            composer.m_VerticalDamping = originalVerticalDampening;
        }
    }

    public void SnapCamera()
    {
        StartCoroutine(SnapCameraToTarget());
    }
}
