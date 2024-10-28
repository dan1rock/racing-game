using UnityEngine;

public class CarController : MonoBehaviour
{
    private Controls _controls;
    private Car _car;

    private void Awake()
    {
        _car = GetComponent<Car>();
        _controls = Controls.Get();
    }

    private void Update()
    {
        if (_car.forceStop) return;
        
        HandlePlayerInput();
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
