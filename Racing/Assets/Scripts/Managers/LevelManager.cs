using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DistantLands.Cozy;
using DistantLands.Cozy.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
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

public class LevelManager : MonoBehaviour
{
    [Header("Stage Settings")]
    [SerializeField] private GameObject pickedCar;
    [SerializeField] private int pickedCarColor;
    [SerializeField] private Weather weather;
    [SerializeField] private DayTime dayTime;
    [SerializeField] public RaceMode raceMode;
    [SerializeField] public bool reverse;
    [SerializeField] public int laps = 3;

    [Header("Surface Grip")] 
    [SerializeField] public float trackGrip = 1f;
    [SerializeField] public float otherGrip = 0.7f;
    
    [Header("Graphics")] 
    [SerializeField] private GameObject grass;

    [Header("Technical")] 
    [SerializeField] private GameObject driftUI;
    [SerializeField] private GameObject timeAttackUI;
    [SerializeField] private GameObject mobileUI;
    [SerializeField] private GameObject wrongDirectionSign;
    [SerializeField] private GameObject directionArrow;
    [SerializeField] private GameObject resetCarUI;
    [SerializeField] private Transform activeCarMarker;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<int> dayTimes;
    [SerializeField] private List<WeatherProfile> weathers;

    [SerializeField] public bool nightMode = false;

    private Transform _startPosition;
    private List<Car> _cars = new();

    private int _activeCar = 0;
    public int currentLap = 1;

    public bool wrongDirection = false;
    public bool wrongDirectionActive = false;
    public bool resetCar = false;
    public bool carStarted = false;
    private bool _playerFinished = false;

    public CheckPoint lastCheckPoint;

    public event Action OnLapFinish;
    public event Action OnStageFinish;

    private AudioSource _audioSource;
    private Controls _controls;
    private TimeAttackManager _timeAttackManager;
    private DriftManager _driftManager;
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
            reverse = gameManager.stageReverse;
            laps = gameManager.laps;

            if (gameManager.graphicsQuality == QualityLevel.High && grass)
            {
                grass.SetActive(true);
            }
        }
        
        _controls = Controls.Get();
        _cozyWeather = FindObjectOfType<CozyWeather>();
        _audioSource = GetComponent<AudioSource>();
        _timeAttackManager = GetComponent<TimeAttackManager>();
        _driftManager = GetComponent<DriftManager>();
        _reflectionProbe = FindObjectOfType<ReflectionProbe>();

        if (weather is Weather.Rainy or Weather.Snowy) nightMode = true;
        if (dayTime == DayTime.Night) nightMode = true;

        Application.targetFrameRate = 60;

        _startPosition = GameObject.FindWithTag("Respawn").transform;

        if (reverse) _startPosition.forward = -_startPosition.forward;
        
        GameObject playerCar = Instantiate(pickedCar, _startPosition.position, _startPosition.rotation);
        _cars.Add(playerCar.GetComponent<Car>());
        
        if (GameManager.Get())
        {
            _cars[_activeCar].SetColor(GameManager.Get().carColors[pickedCarColor]);
        }
        
        activeCarMarker.position = playerCar.transform.position + playerCar.transform.up;
        cameraTarget.position = playerCar.transform.position;
        cameraTarget.rotation = playerCar.transform.rotation;
        
        foreach (Car car in _cars)
        {
            Destroy(car.GetComponent<CarController>());
        }
        UpdateTargetCar();

        switch (raceMode)
        {
            case RaceMode.TimeAttack:
                timeAttackUI.SetActive(true);
                break;
            case RaceMode.Drift:
                driftUI.SetActive(true);
                break;
        }

        if (Application.isMobilePlatform)
        {
            mobileUI.SetActive(true);
        }
        
        UpdateWeather();
        StartCoroutine(UpdateReflectionProbe(0.1f));
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
            Destroy(_cars[_activeCar].GetComponent<CarController>());
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
        if (!_cars[_activeCar].GetComponent<CarController>())
        {
            _cars[_activeCar].AddComponent<CarController>();
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

    public void OnCheckpoint(CheckPoint checkpoint)
    {
        lastCheckPoint = checkpoint;
        _audioSource.pitch = Random.Range(1.1f, 1.2f);
        _audioSource.Play();
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
    }
}
