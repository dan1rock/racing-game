using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public enum Drivetrain
{
    AWD,
    RWD,
    FWD
}

public class Car : MonoBehaviour
{
    [Header("Wheel physics")]
    [SerializeField] private Transform tire_fr;
    [SerializeField] private Transform tire_fl;
    [SerializeField] private Transform tire_rr;
    [SerializeField] private Transform tire_rl;
    
    [Header("Wheel visuals")]
    [SerializeField] private Transform wheel_fr;
    [SerializeField] private Transform wheel_fl;
    [SerializeField] private Transform wheel_rr;
    [SerializeField] private Transform wheel_rl;
    [SerializeField] private float wheelOffset = 0f;

    private Wheel _wheel_fr;
    private Wheel _wheel_fl;
    private Wheel _wheel_rr;
    private Wheel _wheel_rl;

    [Header("Suspension")] 
    [SerializeField] private float suspensionLength = 1f;
    [SerializeField] private float suspensionRest = 0.5f;
    [SerializeField] private float springStrength = 30f;
    [SerializeField] private float springDamper = 10f;

    [Header("Downforce")] 
    [SerializeField] private float downForce = 1f;
    [SerializeField] private AnimationCurve downforceCurve;

    [Header("Steering")] 
    [SerializeField] private AnimationCurve smoothSteering;
    [SerializeField] private float tireGrip = 0.8f;
    [SerializeField] private float tireMass = 1f;
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private AnimationCurve gripSlipCurve;
    [SerializeField] private AnimationCurve gripSpeedCurve;
    [SerializeField] private float driftTrailTrigger = 0.1f;

    [Header("Acceleration")] 
    [SerializeField] private Drivetrain drivetrain;
    [SerializeField] private float torque = 1f;
    [SerializeField] private float topSpeed = 2f;
    [SerializeField] private float topReverse = 20f;
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private bool useGripInAcceleration = true;

    [Header("Breaks")] 
    [SerializeField] private float breakForce = 5f;

    [Header("Handbrake")] 
    [SerializeField] private float handbrakeForce = 2f;
    [SerializeField] private float handbrakeGripMultiplier = 0.5f;

    [Header("Sounds")] 
    [SerializeField] private float minEnginePitch = 0.2f;
    [SerializeField] private float maxEnginePitch = 1.6f;
    [SerializeField] private AnimationCurve enginePitchCurve;
    
    [SerializeField] private LayerMask layerMask;

    [SerializeField] [ReadOnly] private float speed;
    [SerializeField] [ReadOnly] private float relativeSpeed;

    [SerializeField] public bool playerControlled = false;

    private float _carSpeed;
    private float _acceleration = 0f;
    private float _steering;
    private bool _handbrake = false;
    private bool _torqueWheelContact = false;

    private Controls _controls;
    private AudioSource _audioSource;
    private Rigidbody _rb;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        
        _controls = Controls.Get();

        _wheel_fr = wheel_fr.GetComponentInChildren<Wheel>();
        _wheel_fl = wheel_fl.GetComponentInChildren<Wheel>();
        _wheel_rr = wheel_rr.GetComponentInChildren<Wheel>();
        _wheel_rl = wheel_rl.GetComponentInChildren<Wheel>();
    }

    private void Update()
    {
        if (!playerControlled) return;
        HandleInput();
    }

    private void HandleInput()
    {
        if (_controls.GetKeyDown(ControlKey.ResetCar))
        {
            _rb.MovePosition(transform.position + Vector3.up);
            
            Vector3 rotation = transform.rotation.eulerAngles;
            rotation.x = 0f;
            rotation.z = 0f;
            
            transform.rotation = Quaternion.Euler(rotation);
        }
        
        _acceleration = 0f;
        if (_controls.GetKey(ControlKey.Accelerate))
        {
            _acceleration += 1f;
        }
        
        if (_controls.GetKey(ControlKey.Break))
        {
            _acceleration -= 1f;
        }

        _handbrake = _controls.GetKey(ControlKey.Handbrake);

        if (_controls.GetKey(ControlKey.Left))
        {
            _steering -= 1f * Time.deltaTime;

            if (_steering > 0f) _steering -= 5f * Time.deltaTime;
        }
        
        if (_controls.GetKey(ControlKey.Right))
        {
            _steering += 1f * Time.deltaTime;
            
            if (_steering < 0f) _steering += 5f * Time.deltaTime;
        }

        if (!_controls.GetKey(ControlKey.Right) && !_controls.GetKey(ControlKey.Left))
        {
            float diff = Mathf.Sign(_steering) * 5f * Time.deltaTime;
            if (Mathf.Abs(diff) > Mathf.Abs(_steering))
            {
                _steering = 0f;
            }
            else
            {
                _steering -= diff;
            }
        }

        _steering = Mathf.Clamp(_steering, -1f, 1f);

        float steering = smoothSteering.Evaluate(Mathf.Abs(_steering)) * 25f * Mathf.Sign(_steering);

        steering *= steeringCurve.Evaluate(speed / 100f);

        tire_fr.localRotation = Quaternion.Euler(0f, steering, 0f);
        tire_fl.localRotation = Quaternion.Euler(0f, steering, 0f);
    }

    private void FixedUpdate()
    {
        HandleCarPhysics();
        HandleEngineSound();
    }

    private void HandleCarPhysics()
    {
        _torqueWheelContact = false;
        
        ProcessWheel(tire_fr, _wheel_fr, drivetrain is Drivetrain.FWD or Drivetrain.AWD, false);
        ProcessWheel(tire_fl, _wheel_fl, drivetrain is Drivetrain.FWD or Drivetrain.AWD, false);

        ProcessWheel(tire_rr, _wheel_rr, drivetrain is Drivetrain.RWD or Drivetrain.AWD, true);
        ProcessWheel(tire_rl, _wheel_rl, drivetrain is Drivetrain.RWD or Drivetrain.AWD, true);
        
        // Downforce
            
        _rb.AddForce(-transform.up * (downForce * downforceCurve.Evaluate(relativeSpeed)));
    }

    private void ProcessWheel(Transform tire, Wheel wheel, bool applyTorque, bool isRear)
    {
        Ray ray = new()
        {
            origin = tire.position + tire.up * 0.5f,
            direction = -tire.up
        };
        bool hit = Physics.Raycast(ray, out RaycastHit wheelRay, suspensionLength + 0.5f, layerMask);

        _carSpeed = Vector3.Dot(transform.forward, _rb.velocity);
        speed = Mathf.Abs(_carSpeed);
        float normalizedSpeed = Mathf.Clamp01(speed / topSpeed);
        float normalizedReverse = Mathf.Clamp01(speed / topReverse);
        relativeSpeed = normalizedSpeed;
        
        if (hit)
        {
            // Grip calculation
            
            Vector3 wheelVelocity = _rb.GetPointVelocity(tire.position);
            
            float sign = Mathf.Sign(_carSpeed);
            
            float slipAngle = Vector3.SignedAngle(tire.forward * sign, wheelVelocity, tire.up);
            slipAngle = Mathf.Deg2Rad * Mathf.Abs(slipAngle);
            slipAngle = Mathf.Clamp01(slipAngle);
            //if (!applyTorque) slipAngle = 0f;

            if (speed < 0.5f) slipAngle = 0f;
            
            float grip = tireGrip * gripSpeedCurve.Evaluate(relativeSpeed) * gripSlipCurve.Evaluate(slipAngle);

            bool emitTrail = (slipAngle > driftTrailTrigger || (isRear && _handbrake)) && speed > 0.1f;
            
            wheel.SetTrailState(emitTrail);

            if (applyTorque) _torqueWheelContact = true;
            
            // Suspension
            
            Vector3 springDir = tire.up;

            float offset = suspensionRest - wheelRay.distance + 0.5f;
            float velocity = Vector3.Dot(springDir, wheelVelocity);
            float force = offset * springStrength - velocity * springDamper;

            if (force < 0f) force = 0f;
            _rb.AddForceAtPosition(springDir * force, tire.position);

            wheel.transform.position = tire.position + springDir * (offset + wheelOffset);
            
            // Acceleration

            Vector3 accelerationDir = tire.forward;

            bool accelerate = Mathf.Sign(_carSpeed) == Mathf.Sign(_acceleration);
            float curveValue = _acceleration < 0f ? normalizedReverse : normalizedSpeed;
            float speedFactor = accelerate ? accelerationCurve.Evaluate(curveValue) : 0f;
            float availableTorque = torque * _acceleration * speedFactor;
            if (useGripInAcceleration) availableTorque *= grip;
            if (drivetrain == Drivetrain.AWD) availableTorque *= 0.5f;
            
            if (applyTorque && (!isRear || !_handbrake))
            {
                _rb.AddForceAtPosition(accelerationDir * availableTorque, tire.position);
            }
            
            float accelerationVelocity = Vector3.Dot(accelerationDir, wheelVelocity);
            wheel.SetRotationSpeed(accelerationVelocity);
            
            // Breaks

            bool isBreaking = !accelerate && _acceleration != 0f;
            
            if (isBreaking)
            {
                _rb.AddForceAtPosition(accelerationDir * (breakForce * Mathf.Sign(_acceleration)), tire.position);
            }
            
            // Handbrake

            if (isRear && _handbrake)
            {
                if (!isBreaking)
                {
                    _rb.AddForceAtPosition(-accelerationDir * (handbrakeForce * Mathf.Sign(_carSpeed)), tire.position);
                }

                grip *= handbrakeGripMultiplier;
            }
            
            // Drag

            float drag = Mathf.Abs(_carSpeed);
            if (drag > 1f) drag = 1f;
            drag *= Mathf.Sign(_carSpeed);
            drag *= 0.5f;
            _rb.AddForceAtPosition(-accelerationDir * drag, tire.position);
            
            // Steering

            Vector3 steeringDir = tire.right;

            float steeringVelocity = Vector3.Dot(steeringDir, wheelVelocity);
            float desiredVelocityChange = -steeringVelocity * grip;
            float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
            
            _rb.AddForceAtPosition(steeringDir * (tireMass * desiredAcceleration), tire.position);
        }
        else
        {
            wheel.transform.position = tire.position + tire.up * (suspensionRest - suspensionLength + wheelOffset);

            wheel.SetTrailState(false);
        }
    }

    private float enginePitchFactor = 0f;
    private void HandleEngineSound()
    {
        if (!_audioSource) return;

        bool accelerate = Mathf.Sign(_carSpeed) == Mathf.Sign(_acceleration) && _acceleration != 0f;

        float to = accelerate ? relativeSpeed : relativeSpeed - 0.3f;
        if (_acceleration != 0f && !_torqueWheelContact) to = maxEnginePitch; 
        enginePitchFactor = Mathf.Lerp(enginePitchFactor, to, Time.deltaTime);

        enginePitchFactor = Mathf.Clamp01(enginePitchFactor);
        
        _audioSource.pitch = enginePitchCurve.Evaluate(enginePitchFactor) * (maxEnginePitch - minEnginePitch) + minEnginePitch;
    }
}
