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

    [SerializeField] private TMP_Text scoreText;
    
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

    public bool stageFailed = false;
    public bool wrongDirection = false;
    public bool wrongDirectionActive = false;
    public bool resetCar = false;
    public bool carStarted = false;
    public bool rewind = false;
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

    private void FixedUpdate()
    {
        HandleRewind();
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
        
        if (rewind)
        {
            LockCamera(Time.deltaTime * 10f);
        }
        else
        {
            UnlockCamera();

            if (_controls.GetKeyDown(ControlKey.Rewind))
            {
                StartCoroutine(RewindRoutine());
            }
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

    private readonly List<CheckPoint> _rewindCheckpoints = new();
    private readonly List<int> _rewindLaps = new();
    private void HandleRewind()
    {
        if (rewind)
        {
            RewindState();
        }
        else
        {
            RecordState();
        }
    }

    private void RecordState()
    {
        const int maxRewindSteps = 30 * 60;
        
        _rewindCheckpoints.Insert(0, lastCheckPoint);
        _rewindLaps.Insert(0, currentLap);

        if (_rewindCheckpoints.Count > maxRewindSteps)
        {
            _rewindCheckpoints.RemoveAt(_rewindCheckpoints.Count - 1);
            _rewindLaps.RemoveAt(_rewindLaps.Count - 1);
        }
    }

    private void RewindState()
    {
        if (_rewindCheckpoints.Count <= 1) return;

        lastCheckPoint = _rewindCheckpoints[0];
        _rewindCheckpoints.RemoveAt(0);

        currentLap = _rewindLaps[0];
        _rewindLaps.RemoveAt(0);
        
        lastCheckPoint.Activate(false);
        lastCheckPoint.GetNext().Activate(true);
        lastCheckPoint.GetNext().GetNext().Activate(false);
    }

    private IEnumerator RewindRoutine()
    {
        if (_playerFinished) yield break;
        
        rewind = true;

        while (true)
        {
            while (Time.timeScale < 1f)
            {
                Time.timeScale = Mathf.Clamp01(Time.timeScale + Time.unscaledDeltaTime * 2f);
                yield return null;
            }

            yield return new WaitForSeconds(2f);

            while (Time.timeScale > 0f)
            {
                Time.timeScale = Mathf.Clamp01(Time.timeScale - Time.unscaledDeltaTime * 2f);
                yield return null;
            }

            while (true)
            {
                if (_controls.GetKey(ControlKey.Accelerate))
                {
                    rewind = false;
                    break;
                }
                
                if (_controls.GetKey(ControlKey.Rewind))
                {
                    break;
                }
                
                yield return null;
            }

            while (Time.timeScale < 1f)
            {
                Time.timeScale = Mathf.Clamp01(Time.timeScale + Time.unscaledDeltaTime * 2f);
                yield return null;
            }
            
            if (!rewind) break;
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
        if (_playerFinished) return;
        
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
            _cars[_activeCar].GetComponent<CarPlayer>().disablePlayer = true;
            _cars[_activeCar].isDriftCar = false;
            CarBot bot = _cars[_activeCar].transform.AddComponent<CarBot>();
            bot.playerAutopilot = true;
            bot.ActivateBot();
        }
    }

    public void OnPlayerFinish()
    {
        if (_playerFinished) return;
        
        OnStageFinish?.Invoke();
        
        DisableControls(true);
        resetCarUI.SetActive(false);
        WrongDirection(false);

        _playerFinished = true;
        
        if (GameManager.Get()?.challengeManager) CheckChallengeCompletion();
    }

    public void FailStage()
    {
        if (_playerFinished) return;

        stageFailed = true;
        OnPlayerFinish();
    }

    private IEnumerator UpdateChallengeInformation()
    {
        if (GameManager.Get().challengeManager.collectorChallenge)
        {
            StartCoroutine(UpdateCollectablesChallenge());
            yield break;
        }
        
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

    private IEnumerator UpdateCollectablesChallenge()
    {
        ChallengeCollector challengeCollector = FindFirstObjectByType<ChallengeCollector>();

        while (true)
        {
            scoreText.text = $"{challengeCollector.totalCollected} / {challengeCollector.totalCollectibles}";
            yield return new WaitForSeconds(0.1f);
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

    private CinemachineTransposer _transposer;
    private CinemachineFramingTransposer _framingTransposer;
    private CinemachineComposer _composer;

    private float _originalDamping;
    private float _originalYDamping;
    private float _originalZDamping;

    private float _originalFramingXDamping;
    private float _originalFramingYDamping;
    private float _originalFramingZDamping;

    private float _originalHorizontalDampening;
    private float _originalVerticalDampening;
    
    private void LockCamera(float lerpSpeed = 1f)
    {
        cameraTarget.rotation = player.transform.rotation;
        
        if (!_transposer)
        {
            _transposer = _cinemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
            
            _originalDamping = _transposer ? _transposer.m_XDamping : 0;
            _originalYDamping = _transposer ? _transposer.m_YDamping : 0;
            _originalZDamping = _transposer ? _transposer.m_ZDamping : 0;
        }
        
        if (!_framingTransposer)
        {
            _framingTransposer = _cinemachineCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            
            _originalFramingXDamping = _framingTransposer ? _framingTransposer.m_XDamping : 0;
            _originalFramingYDamping = _framingTransposer ? _framingTransposer.m_YDamping : 0;
            _originalFramingZDamping = _framingTransposer ? _framingTransposer.m_ZDamping : 0;
        }
        
        if (!_composer)
        {
            _composer = _cinemachineCamera.GetCinemachineComponent<CinemachineComposer>();

            _originalHorizontalDampening = _composer ? _composer.m_HorizontalDamping : 0;
            _originalVerticalDampening = _composer ? _composer.m_VerticalDamping : 0;
        }
        
        if (_transposer)
        {
            _transposer.m_XDamping = Mathf.Lerp(_transposer.m_XDamping, 0, lerpSpeed);
            _transposer.m_YDamping = Mathf.Lerp(_transposer.m_YDamping, 0, lerpSpeed);
            _transposer.m_ZDamping = Mathf.Lerp(_transposer.m_ZDamping, 0, lerpSpeed);
        }

        if (_framingTransposer)
        {
            _framingTransposer.m_XDamping = Mathf.Lerp(_framingTransposer.m_XDamping, 0, lerpSpeed);
            _framingTransposer.m_YDamping = Mathf.Lerp(_framingTransposer.m_YDamping, 0, lerpSpeed);
            _framingTransposer.m_ZDamping = Mathf.Lerp(_framingTransposer.m_ZDamping, 0, lerpSpeed);
        }

        if (_composer)
        {
            _composer.m_HorizontalDamping = Mathf.Lerp(_composer.m_HorizontalDamping, 0, lerpSpeed);
            _composer.m_VerticalDamping = Mathf.Lerp(_composer.m_VerticalDamping, 0, lerpSpeed);
        }
    }

    private void UnlockCamera()
    {
        if (_transposer)
        {
            _transposer.m_XDamping = _originalDamping;
            _transposer.m_YDamping = _originalYDamping;
            _transposer.m_ZDamping = _originalZDamping;
        }

        if (_framingTransposer)
        {
            _framingTransposer.m_XDamping = _originalFramingXDamping;
            _framingTransposer.m_YDamping = _originalFramingYDamping;
            _framingTransposer.m_ZDamping = _originalFramingZDamping;
        }
        
        if (_composer)
        {
            _composer.m_HorizontalDamping = _originalHorizontalDampening;
            _composer.m_VerticalDamping = _originalVerticalDampening;
        }
    }
    
    private IEnumerator SnapCameraToTarget()
    {
        LockCamera();
        
        yield return null;
        
        UnlockCamera();   
    }

    public void SnapCamera()
    {
        StartCoroutine(SnapCameraToTarget());
    }
}
