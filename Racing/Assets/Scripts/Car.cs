using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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
    [SerializeField] private float steeringMaxAngle = 25f;
    [SerializeField] private float speedSteeringDampening = 3f;
    [SerializeField] private float frontTireGrip = 0.8f;
    [SerializeField] private float rearTireGrip = 0.8f;
    [SerializeField] private float tireMass = 1f;
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private AnimationCurve gripSlipCurve;
    [SerializeField] private AnimationCurve gripSpeedCurve;
    [SerializeField] private float driftTrailTrigger = 0.1f;
    [SerializeField] public bool isDriftCar = false;
    [SerializeField] private float driftCounterSteering = 30f;

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
    [SerializeField] private List<float> relativeGears;
    [SerializeField] private List<float> gearMinPitch;
    [SerializeField] private AudioSource gearShiftAudio;
    [SerializeField] private AudioClip gearShiftClip;
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField] private AudioClip engineStopClip;

    [Header("Misc")]
    [SerializeField] private LayerMask layerMask;

    [SerializeField] [ReadOnly] private float speed;
    [SerializeField] [ReadOnly] private float relativeSpeed;

    [SerializeField] public bool playerControlled = false;

    private float _carSpeed;
    private float _acceleration = 0f;
    private float _driftCounterSteering;
    private float _steering;
    private float _speedSteeringRatio;
    private float _rearSlipAngle;

    private int _currentGear = 1;

    private bool _engineOn = false;
    private bool _engineStarting = false;
    private bool _pendingReset = false;
    private bool _handbrake = false;
    private bool _torqueWheelContact = false;
    private bool _wheelContact = false;

    private bool _breakLight = false;
    private bool _reverseLight = false;

    private Controls _controls;
    private DriftCounter _driftCounter;
    private AudioSource _audioSource;
    private Rigidbody _rb;

    private Material _breakLightMat;
    private Color _breakEmissionColor;
    
    private Material _reverseLightMat;
    private Color _reverseEmissionColor;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.volume = 0f;
        _driftCounter = FindObjectOfType<DriftCounter>();
        
        _controls = Controls.Get();

        _wheel_fr = wheel_fr.GetComponentInChildren<Wheel>();
        _wheel_fl = wheel_fl.GetComponentInChildren<Wheel>();
        _wheel_rr = wheel_rr.GetComponentInChildren<Wheel>();
        _wheel_rl = wheel_rl.GetComponentInChildren<Wheel>();

        _speedSteeringRatio = 1f / speedSteeringDampening;
        
        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.name.Contains("Break Light"))
                {
                    _breakLightMat = mat;
                    _breakEmissionColor = _breakLightMat.GetColor(EmissionColor);
                }
                
                if (mat.name.Contains("Reverse Light"))
                {
                    _reverseLightMat = mat;
                    _reverseEmissionColor = _reverseLightMat.GetColor(EmissionColor);
                }
            }
        }
    }

    private void Update()
    {
        if (!_engineStarting)
        {
            _engineVolume = Mathf.Lerp(_engineVolume, _engineOn ? 0.7f : 0f, Time.deltaTime * 3f);
        }
        _audioSource.volume = _engineVolume;
        
        if (!playerControlled) return;
        HandleInput();
    }

    private void HandleInput()
    {
        if (_controls.GetKeyDown(ControlKey.ResetCar))
        {
            _pendingReset = true;
        }

        if (_controls.GetKeyDown(ControlKey.StopEngine))
        {
            StartCoroutine(StopEngine());
        }

        _acceleration = 0f;
        if (_controls.GetKey(ControlKey.Accelerate))
        {
            _acceleration += 1f;
            if (!_engineOn) StartCoroutine(StartEngine());
        }

        if (_controls.GetKey(ControlKey.Break))
        {
            _acceleration -= 1f;
            if (!_engineOn) StartCoroutine(StartEngine());
        }

        _handbrake = _controls.GetKey(ControlKey.Handbrake);

        float steeringLimit = steeringCurve.Evaluate(speed / 100f);
        if (_carSpeed < 0f) steeringLimit = 1f;
        float steeringRatio = steeringLimit * _speedSteeringRatio + (1f - _speedSteeringRatio);

        if (_controls.GetKey(ControlKey.Left))
        {
            _steering -= 1f * Time.deltaTime * steeringRatio;

            if (_steering > 0f) _steering -= 5f * Time.deltaTime;
        }

        if (_controls.GetKey(ControlKey.Right))
        {
            _steering += 1f * Time.deltaTime * steeringRatio;

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

        _steering = Mathf.Clamp(_steering, -steeringLimit, steeringLimit);

        float steering = smoothSteering.Evaluate(Mathf.Abs(_steering)) * steeringMaxAngle * Mathf.Sign(_steering) +
                         _driftCounterSteering;

        tire_fr.localRotation = Quaternion.Euler(0f, steering, 0f);
        tire_fl.localRotation = Quaternion.Euler(0f, steering, 0f);
    }

    private void FixedUpdate()
    {
        _breakLight = false;
        _reverseLight = false;
        
        if (_pendingReset)
        {
            _rb.MovePosition(transform.position + Vector3.up);
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            
            Vector3 rotation = transform.rotation.eulerAngles;
            rotation.x = 0f;
            rotation.z = 0f;
            
            transform.rotation = Quaternion.Euler(rotation);

            _pendingReset = false;
        }
        
        HandleCarPhysics();
        HandleEngineSound();
        HandleDrift();
        HandleLights();
    }

    private void HandleCarPhysics()
    {
        _torqueWheelContact = false;
        _wheelContact = false;
        
        // Process gears

        HandleGears();

        // Process wheels
        
        ProcessWheel(tire_fr, _wheel_fr, drivetrain is Drivetrain.FWD or Drivetrain.AWD, false);
        ProcessWheel(tire_fl, _wheel_fl, drivetrain is Drivetrain.FWD or Drivetrain.AWD, false);

        ProcessWheel(tire_rr, _wheel_rr, drivetrain is Drivetrain.RWD or Drivetrain.AWD, true);
        ProcessWheel(tire_rl, _wheel_rl, drivetrain is Drivetrain.RWD or Drivetrain.AWD, true);
        
        // Downforce
        
        if (_wheelContact)
        {
            _rb.AddForce(-transform.up * (downForce * downforceCurve.Evaluate(relativeSpeed)));
        }
        
        // Drift counter steering

        if (isDriftCar)
        {
            float angle = Vector3.SignedAngle(transform.forward, _rb.velocity, Vector3.up);
            _driftCounterSteering = Mathf.Abs(angle) < 90f && speed > 1f && _wheelContact
                ? angle * Mathf.Deg2Rad * driftCounterSteering 
                : 0f;
        }
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
            if (isRear) _rearSlipAngle = slipAngle;
            slipAngle = Mathf.Clamp01(slipAngle);

            if (speed < 0.5f) slipAngle = 0f;

            float tireGrip = isRear ? rearTireGrip : frontTireGrip;
            if (isDriftCar && isRear && _acceleration != 0f) tireGrip *= 0.6f;
            float grip = tireGrip * gripSpeedCurve.Evaluate(relativeSpeed) * gripSlipCurve.Evaluate(slipAngle);

            bool emitTrail = (slipAngle > driftTrailTrigger || (isRear && _handbrake)) && speed > 1f;
            
            wheel.SetTrailState(emitTrail, Mathf.Abs(Vector3.Dot(wheelVelocity, tire.right)));

            if (applyTorque) _torqueWheelContact = true;
            _wheelContact = true;

            wheel.isContactingTrack = wheelRay.transform.gameObject.layer == 7;
            
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
            
            if (applyTorque && (!isRear || !_handbrake) && _engineOn)
            {
                _rb.AddForceAtPosition(accelerationDir * availableTorque, tire.position);
                if (availableTorque < 0f) _reverseLight = true;
            }
            
            float accelerationVelocity = Vector3.Dot(accelerationDir, wheelVelocity);
            wheel.SetRotationSpeed(accelerationVelocity);
            
            // Breaks

            bool isBreaking = !accelerate && _acceleration != 0f;
            
            if (isBreaking && (!isRear || !_handbrake) && !(_handbrake && speed < 0.5f))
            {
                _rb.AddForceAtPosition(accelerationDir * (breakForce * Mathf.Sign(_acceleration)), tire.position);
                _breakLight = true;
            }
            else
            {
                isBreaking = false;
            }
            
            // Handbrake

            if (isRear && _handbrake)
            {
                if (!isBreaking)
                {
                    _rb.AddForceAtPosition(-accelerationDir * (handbrakeForce * Mathf.Sign(_carSpeed)), tire.position);
                    _breakLight = true;
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

            wheel.SetTrailState(false, 0f);
            wheel.isContactingTrack = false;
        }
    }

    private float _enginePitchFactor = 0f;
    private float _engineVolume = 0f;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void HandleEngineSound()
    {
        if (!_audioSource) return;
        if (!_engineOn) return;
        
        bool accelerate = Mathf.Sign(_carSpeed) == Mathf.Sign(_acceleration) && _acceleration != 0f;

        float bottomBoundary = relativeGears[_currentGear - 1];
        float topBoundary = _currentGear < relativeGears.Count ? relativeGears[_currentGear] : 1f;
        float gearPitch = (relativeSpeed - bottomBoundary) / (topBoundary - bottomBoundary);
        
        float to = accelerate 
            ? gearPitch + (drivetrain == Drivetrain.FWD || !_wheelContact || speed < 1f ? 0f : _rearSlipAngle) 
            : gearPitch - 0.3f;

        if (_acceleration != 0f && !_torqueWheelContact) to = maxEnginePitch;

        float lerpSpeed = _enginePitchFactor < to ? 1f : 2f;
        _enginePitchFactor = Mathf.Lerp(_enginePitchFactor, to, Time.deltaTime * lerpSpeed);

        _enginePitchFactor = Mathf.Clamp01(_enginePitchFactor);

        float minPitch = gearMinPitch[_currentGear - 1];
        _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - minPitch) + minPitch;

        _audioSource.volume = _engineVolume;
    }

    private void HandleGears()
    {
        if (!_engineOn) return;
        
        if (relativeGears[_currentGear - 1] > relativeSpeed)
        {
            gearShiftAudio.clip = gearShiftClip;
            gearShiftAudio.pitch = Random.Range(0.8f, 1.2f);
            gearShiftAudio.Play();
            _currentGear--;
        }
        
        if (_currentGear >= relativeGears.Count) return;
        
        if (relativeGears[_currentGear] < relativeSpeed)
        {
            gearShiftAudio.clip = gearShiftClip;
            gearShiftAudio.pitch = Random.Range(0.8f, 1.2f);
            gearShiftAudio.Play();
            _currentGear++;
        }
    }

    private void HandleDrift()
    {
        if (!isDriftCar) return;
        if (!_wheelContact) return;

        int tiresOnTrack = 0;

        if (_wheel_fl.isContactingTrack) tiresOnTrack++;
        if (_wheel_fr.isContactingTrack) tiresOnTrack++;
        if (_wheel_rl.isContactingTrack) tiresOnTrack++;
        if (_wheel_rr.isContactingTrack) tiresOnTrack++;
        
        if (tiresOnTrack < 3) return;
        
        _driftCounter.ProcessDrift(_rb.velocity.magnitude, _rearSlipAngle);
    }

    private void HandleLights()
    {
        if (speed < 1f) _breakLight = true;
        
        _breakLightMat?.SetColor(EmissionColor, _breakLight ? _breakEmissionColor : Color.black);
        _reverseLightMat?.SetColor(EmissionColor, _reverseLight ? _reverseEmissionColor : Color.black);
    }

    private IEnumerator StartEngine()
    {
        if (_engineStarting) yield break;
        
        _engineStarting = true;
        
        gearShiftAudio.clip = engineStartClip;
        gearShiftAudio.pitch = 1f;
        gearShiftAudio.Play();

        yield return new WaitForSeconds(0.4f);

        _enginePitchFactor = 0f;
        _audioSource.pitch = minEnginePitch;

        while (_engineVolume < 0.4f)
        {
            _engineVolume = Mathf.Lerp(_engineVolume, 0.7f, Time.deltaTime * 3f);

            yield return null;
        }

        while (_enginePitchFactor < 0.3f)
        {
            _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - minEnginePitch) +
                                 minEnginePitch;
            _engineVolume = Mathf.Lerp(_engineVolume, 0.7f, Time.deltaTime * 3f);

            _enginePitchFactor += Time.deltaTime * 0.5f;

            yield return null;
        }
        
        while (_enginePitchFactor > 0f)
        {
            _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - 0.2f) + 0.2f;
            _engineVolume = Mathf.Lerp(_engineVolume, 0.7f, Time.deltaTime * 3f);

            _enginePitchFactor -= Time.deltaTime * 0.3f;
            _enginePitchFactor = Mathf.Clamp01(_enginePitchFactor);

            yield return null;
        }

        _engineOn = true;
        _engineStarting = false;
    }

    private IEnumerator StopEngine()
    {
        if (_engineStarting) yield break;
        
        _engineStarting = true;
        _engineOn = false;
        
        _audioSource.pitch = minEnginePitch;

        while (_enginePitchFactor > 0f)
        {
            _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - minEnginePitch) +
                                 minEnginePitch;

            _enginePitchFactor -= Time.deltaTime * 0.5f;

            yield return null;
        }
        
        gearShiftAudio.clip = engineStopClip;
        gearShiftAudio.pitch = 1f;
        gearShiftAudio.Play();

        _engineStarting = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (isDriftCar)
        {
            if (other.impulse.magnitude > 0.5f && other.gameObject.layer != 7)
            {
                _driftCounter.DriftFailed();
            }
        }
    }
}
