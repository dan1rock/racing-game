using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private List<Car> cars;
    [SerializeField] private GameObject driftUI;
    [SerializeField] private GameObject mobileUI;
    [SerializeField] private Transform activeCarMarker;

    [SerializeField] public bool nightMode = false;

    private int _activeCar = 0;

    private Controls _controls;

    private void Awake()
    {
        _controls = Controls.Get();

        Application.targetFrameRate = 60;

        foreach (Car car in cars)
        {
            car.playerControlled = false;
        }
        UpdateTargetCar();

        if (Application.isMobilePlatform)
        {
            mobileUI.SetActive(true);
        }
    }

    private void Update()
    {
        HandleInput();

        activeCarMarker.position = cars[_activeCar].transform.position;
    }

    private void HandleInput()
    {
        if (_controls.GetKeyDown(ControlKey.CycleCar))
        {
            cars[_activeCar].playerControlled = false;
            _activeCar += 1;
            if (_activeCar >= cars.Count) _activeCar = 0;
            UpdateTargetCar();
        }
    }

    private void UpdateTargetCar()
    {
        cars[_activeCar].playerControlled = true;
        virtualCamera.Follow = cars[_activeCar].transform;
        virtualCamera.LookAt = cars[_activeCar].transform;
        driftUI.SetActive(cars[_activeCar].isDriftCar);
    }
}
