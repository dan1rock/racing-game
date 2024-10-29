using System;
using UnityEngine;

public class CarController : MonoBehaviour
{
    private Controls _controls;
    private Car _car;
    private LevelManager _levelManager;
    private RacingLine _racingLine;

    public int currentNodeId;

    private void Awake()
    {
        _car = GetComponent<Car>();
        _controls = Controls.Get();
        _levelManager = FindObjectOfType<LevelManager>();
        _racingLine = FindObjectOfType<RacingLine>();

        _levelManager.player = this;
    }

    private void Start()
    {
        currentNodeId = _racingLine.GetNearestNodeID(transform.position);
    }

    private void Update()
    {
        if (_car.forceStop) return;
        
        HandlePlayerInput();
    }

    private void FixedUpdate()
    {
        HandleRacingLine();
    }

    private void HandlePlayerInput()
    {
        if (_controls.GetKeyDown(ControlKey.ResetCar))
        {
            _car.InvokeReset(false);
        }

        if (_controls.GetKeyDown(ControlKey.StopEngine))
        {
            _car.StartCoroutine(_car.StopEngine());
        }

        _car.accelInput = 0f;
        if (_controls.GetKey(ControlKey.Accelerate))
        {
            _car.accelInput += 1f;
            if (!_car.engineOn) StartCar();
        }

        if (_controls.GetKey(ControlKey.Break))
        {
            _car.accelInput -= 1f;
            if (!_car.engineOn) StartCar();
        }

        _car.handbrake = _controls.GetKey(ControlKey.Handbrake);

        float steeringLimit = _car.steeringCurve.Evaluate(_car.speed / 100f);
        if (_car.carSpeed < 0f) steeringLimit = 1f;
        float steeringRatio = steeringLimit * _car.speedSteeringRatio + (1f - _car.speedSteeringRatio);

        if (_controls.GetKey(ControlKey.Left))
        {
            _car.steering -= 1f * Time.deltaTime * steeringRatio;

            if (_car.steering > 0f) _car.steering -= 5f * Time.deltaTime;
        }

        if (_controls.GetKey(ControlKey.Right))
        {
            _car.steering += 1f * Time.deltaTime * steeringRatio;

            if (_car.steering < 0f) _car.steering += 5f * Time.deltaTime;
        }

        if (!_controls.GetKey(ControlKey.Right) && !_controls.GetKey(ControlKey.Left))
        {
            float diff = Mathf.Sign(_car.steering) * 5f * Time.deltaTime;
            if (Mathf.Abs(diff) > Mathf.Abs(_car.steering))
            {
                _car.steering = 0f;
            }
            else
            {
                _car.steering -= diff;
            }
        }

        _car.burnout = _controls.GetKey(ControlKey.Accelerate) && _controls.GetKey(ControlKey.Break); 

        _car.steering = Mathf.Clamp(_car.steering, -steeringLimit, steeringLimit);
    }
    
    private void HandleRacingLine()
    {
        if (!_racingLine) return;
        
        int nodesN = _racingLine.orderedNodes.Count;
        
        int prev = currentNodeId - 1;
        if (prev < 0) prev = nodesN - 1;

        int next = currentNodeId + 1;
        if (next >= nodesN) next = 0;

        if (_levelManager.reverse) (next, prev) = (prev, next);

        float prevDistance = (_racingLine.orderedNodes[prev].position - transform.position).magnitude;
        float currentDistance = (_racingLine.orderedNodes[currentNodeId].position - transform.position).magnitude;

        if (currentDistance < prevDistance)
        {
            currentNodeId = next;
        }
    }

    private void StartCar()
    {
        _car.StartCoroutine(_car.StartEngine());

        CarBot[] bots = FindObjectsOfType<CarBot>();
        
        foreach (CarBot bot in bots)
        {
            bot.ActivateBot();
        }
    }
}
