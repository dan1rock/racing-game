using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private List<Car> cars;

    private int _activeCar = 0;

    private void Awake()
    {
        UpdateTargetCar();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.T))
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
    }
}
