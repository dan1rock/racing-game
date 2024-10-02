using System;
using System.Collections.Generic;
using Cinemachine;
using DistantLands.Cozy;
using DistantLands.Cozy.Data;
using UnityEngine;

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

    [Header("Technical")] 
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private GameObject driftUI;
    [SerializeField] private GameObject mobileUI;
    [SerializeField] private Transform activeCarMarker;
    [SerializeField] private List<int> dayTimes;
    [SerializeField] private List<WeatherProfile> weathers;

    [SerializeField] public bool nightMode = false;

    private Transform _startPosition;
    private List<Car> _cars = new();

    private int _activeCar = 0;

    private Controls _controls;
    private CozyWeather _cozyWeather;

    private void Awake()
    {
        if (GameManager.Get())
        {
            GameManager gameManager = GameManager.Get();
            pickedCar = gameManager.car;
            weather = gameManager.weather;
            dayTime = gameManager.dayTime;
        }
        
        _controls = Controls.Get();
        _cozyWeather = FindObjectOfType<CozyWeather>();

        if (weather is Weather.Rainy or Weather.Snowy) nightMode = true;
        if (dayTime == DayTime.Night) nightMode = true;

        Application.targetFrameRate = 60;

        _startPosition = GameObject.FindWithTag("Respawn").transform;
        GameObject playerCar = Instantiate(pickedCar, _startPosition.position, _startPosition.rotation);
        _cars.Add(playerCar.GetComponent<Car>());
        
        foreach (Car car in _cars)
        {
            car.playerControlled = false;
        }
        UpdateTargetCar();

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

        activeCarMarker.position = _cars[_activeCar].transform.position;
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

    private void UpdateTargetCar()
    {
        _cars[_activeCar].playerControlled = true;
        virtualCamera.Follow = _cars[_activeCar].transform;
        virtualCamera.LookAt = _cars[_activeCar].transform;
        driftUI.SetActive(_cars[_activeCar].isDriftCar);
    }
}
