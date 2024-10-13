using System;
using System.Collections.Generic;
using Cinemachine;
using DistantLands.Cozy;
using DistantLands.Cozy.Data;
using UnityEngine;
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
    [SerializeField] private Weather weather;
    [SerializeField] private DayTime dayTime;
    [SerializeField] public RaceMode raceMode;
    [SerializeField] public bool reverse;

    [Header("Graphics")] 
    [SerializeField] private GameObject grass;

    [Header("Technical")] 
    [SerializeField] private GameObject driftUI;
    [SerializeField] private GameObject timeAttackUI;
    [SerializeField] private GameObject mobileUI;
    [SerializeField] private GameObject wrongDirectionSign;
    [SerializeField] private GameObject resetCarUI;
    [SerializeField] private Transform activeCarMarker;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<int> dayTimes;
    [SerializeField] private List<WeatherProfile> weathers;

    [SerializeField] public bool nightMode = false;

    private Transform _startPosition;
    private List<Car> _cars = new();

    private int _activeCar = 0;

    public bool wrongDirection = false;
    public bool resetCar = false;
    public bool carStarted = false;

    public Transform lastCheckPoint;

    private AudioSource _audioSource;
    private Controls _controls;
    private TimeAttackManager _timeAttackManager;
    private CozyWeather _cozyWeather;

    private void Awake()
    {
        if (GameManager.Get())
        {
            GameManager gameManager = GameManager.Get();
            pickedCar = gameManager.car;
            weather = gameManager.weather;
            dayTime = gameManager.dayTime;
            raceMode = gameManager.raceMode;
            reverse = gameManager.stageReverse;

            if (gameManager.graphicsQuality == QualityLevel.High && grass)
            {
                grass.SetActive(true);
            }
        }
        
        _controls = Controls.Get();
        _cozyWeather = FindObjectOfType<CozyWeather>();
        _audioSource = GetComponent<AudioSource>();
        _timeAttackManager = GetComponent<TimeAttackManager>();

        if (weather is Weather.Rainy or Weather.Snowy) nightMode = true;
        if (dayTime == DayTime.Night) nightMode = true;

        Application.targetFrameRate = 60;

        _startPosition = GameObject.FindWithTag("Respawn").transform;

        if (reverse) _startPosition.forward = -_startPosition.forward;
        
        GameObject playerCar = Instantiate(pickedCar, _startPosition.position, _startPosition.rotation);
        _cars.Add(playerCar.GetComponent<Car>());
        
        activeCarMarker.position = playerCar.transform.position + playerCar.transform.up;
        cameraTarget.position = playerCar.transform.position;
        cameraTarget.rotation = playerCar.transform.rotation;
        
        foreach (Car car in _cars)
        {
            car.playerControlled = false;
        }
        UpdateTargetCar();

        switch (raceMode)
        {
            case RaceMode.Race:
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
    }
    
    private void UpdateWeather()
    {
        int time = dayTimes[(int)dayTime];
        if (weather is Weather.Rainy or Weather.Snowy && dayTime == DayTime.Night) time += 1;
        _cozyWeather.timeModule.currentTime = new MeridiemTime(time, 0);
        
        CozyWeather.instance.weatherModule.ecosystem.SetWeather(weathers[(int) weather]);
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
            _cars[_activeCar].playerControlled = false;
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
        _cars[_activeCar].playerControlled = true;
    }

    public Car GetActiveCar()
    {
        return _cars[_activeCar];
    }

    public void WrongDirection(bool state)
    {
        wrongDirectionSign.SetActive(state);
        wrongDirection = state;
    }

    public void ResetCar(bool state)
    {
        if (resetCar == state) return;
        
        resetCarUI.SetActive(state);
        resetCar = state;
    }

    public void OnCheckpoint(Transform checkpoint)
    {
        lastCheckPoint = checkpoint;
        _audioSource.pitch = Random.Range(1.1f, 1.2f);
        _audioSource.Play();
    }

    public void OnLapFinish()
    {
        _timeAttackManager.OnLapFinish();
    }

    public void OnCarStarted()
    {
        carStarted = true;
    }
}
