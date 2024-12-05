using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public enum Drivetrain
{
    AWD,
    RWD,
    FWD
}

public class Car : MonoBehaviour
{
    [Header("Car Info")]
    [SerializeField] public string carName = "Car";
    [SerializeField] public List<Material> randomColorPool;
    [Range(0f, 1f)] [SerializeField] public float maxSpeed = 1f;
    [Range(0f, 1f)] [SerializeField] public float acceleration = 1f;
    [Range(0f, 1f)] [SerializeField] public float handling = 1f;
    [Range(0f, 1f)] [SerializeField] public float difficulty = 1f;
    
    private Transform _tireFr;
    private Transform _tireFl;
    private Transform _tireRr;
    private Transform _tireRl;
    
    private Transform _wheelFr;
    private Transform _wheelFl;
    private Transform _wheelRr;
    private Transform _wheelRl;
    
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
    [SerializeField] public AnimationCurve steeringCurve;
    [SerializeField] private AnimationCurve gripSlipCurve;
    [SerializeField] private AnimationCurve gripSpeedCurve;
    [SerializeField] private float driftTrailTrigger = 0.1f;
    [SerializeField] public bool isDriftCar = false;
    [SerializeField] private float driftCounterSteering = 30f;
    [SerializeField] private float botCounterSteering = 30f;

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

    private AudioSource _gearShiftSource;
    
    [SerializeField] private AudioClip gearShiftClip;
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField] private AudioClip engineStopClip;
    
    [Header("Misc")] 
    [SerializeField] private LayerMask layerMask;

    [SerializeField] [ReadOnly] public float speed;
    [SerializeField] [ReadOnly] private float relativeSpeed;
    
    [SerializeField] public bool menuMode = false;
    [SerializeField] private bool showcaseMode = false;

    [HideInInspector] public float carSpeed;
    [HideInInspector] public float accelInput = 0f;
    [HideInInspector] public bool burnout = false;
    [HideInInspector] public float steering;
    [HideInInspector] public float speedSteeringRatio;
    
    private float _driftCounterSteering;
    private float _rearSlipAngle;
    private float _carAngle;
    
    private float _trackGrip = 1f;
    private float _otherGrip = 0.5f;

    private int _currentGear = 1;

    [HideInInspector] public bool isBot = false;
    [HideInInspector] public bool engineOn = false;
    [HideInInspector] public bool handbrake = false;
    [HideInInspector] public bool forceStop = false;
    
    private bool _nightMode = false;
    private bool _engineStarting = false;
    private bool _pendingReset = false;
    private bool _forceReset = false;
    private bool _torqueWheelContact = false;
    
    [HideInInspector] public bool wheelContact = false;
    [HideInInspector] public Vector3 rbVelocity;

    private bool _breakLight = false;
    private bool _reverseLight = false;

    private GameObject _frontLightSource;

    private LevelManager _levelManager;
    private DriftManager _driftManager;
    private AudioSource _audioSource;
    private Rigidbody _rb;

    private Material _breakLightMat;
    private Material _breakFlareMat;
    private Color _redLightEmissionColor;
    private Color _breakEmissionColor;
    private Color _breakFlareEmissionColor = Color.black;
    
    private Material _reverseLightMat;
    private Color _reverseEmissionColor;

    private Material _frontLightMat;
    private Color _frontEmissionColor;

    private const float MaxEngineVolume = 0.3f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.volume = 0f;
        _driftManager = FindFirstObjectByType<DriftManager>();
        _levelManager = FindFirstObjectByType<LevelManager>();
        
        if (_levelManager)
        {
            _nightMode = _levelManager.nightMode;
            _trackGrip = _levelManager.trackGrip;
            _otherGrip = _levelManager.otherGrip;
        }

        _gearShiftSource = transform.Find("SFX").GetComponent<AudioSource>();
        
        _tireFr = transform.Find("wheel_fr");
        _tireFl = transform.Find("wheel_fl");
        _tireRr = transform.Find("wheel_br");
        _tireRl = transform.Find("wheel_bl");

        _wheelFr = _tireFr.Find("wheel_holder_fr");
        _wheelFl = _tireFl.Find("wheel_holder_fl");
        _wheelRr = _tireRr.Find("wheel_holder_br");
        _wheelRl = _tireRl.Find("wheel_holder_bl");
        
        _wheel_fr = _wheelFr.GetComponentInChildren<Wheel>();
        _wheel_fl = _wheelFl.GetComponentInChildren<Wheel>();
        _wheel_rr = _wheelRr.GetComponentInChildren<Wheel>();
        _wheel_rl = _wheelRl.GetComponentInChildren<Wheel>();

        speedSteeringRatio = 1f / speedSteeringDampening;

        SetUpLights();

        WheelTrail[] trails = gameObject.GetComponentsInChildren<WheelTrail>();
        foreach (WheelTrail wheelTrail in trails)
        {
            wheelTrail.car = this;
        }
        
        if (menuMode)
        {
            SetMenuMode();
        }

        if (showcaseMode)
        {
            SetShowcaseMode();
        }
    }

    private void SetUpLights()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.name.Contains("Break Light"))
                {
                    _breakLightMat = mat;
                }

                if (mat.name.Contains("Break Flare"))
                {
                    _breakFlareMat = mat;
                    _redLightEmissionColor = _breakFlareMat.GetColor(EmissionColor);
                    _breakEmissionColor = _redLightEmissionColor * 2f;
                    _redLightEmissionColor *= 0.5f;
                }

                if (mat.name.Contains("Reverse Light"))
                {
                    _reverseLightMat = mat;
                    _reverseEmissionColor = _reverseLightMat.GetColor(EmissionColor);
                }
                
                if (mat.name.Contains("Frontlights"))
                {
                    _frontLightMat = mat;
                    _frontEmissionColor = _frontLightMat.GetColor(EmissionColor);
                    _frontLightMat.SetColor(EmissionColor, Color.black);
                }
            }
        }

        _frontLightSource = GetComponentInChildren<Light>()?.transform.parent.gameObject;
        _frontLightSource?.SetActive(false);
    }

    private int _mainColorMatId = -1;
    public void SetColor(Material material)
    {
        Renderer carRenderer = GetComponentInChildren<Renderer>();

        if (_mainColorMatId == -1)
        {
            for (int i = 0; i < carRenderer.materials.Length; i++)
            {
                if (!carRenderer.materials[i].name.Contains("Main")) continue;

                _mainColorMatId = i;
                break;
            }
        }

        Material newMaterialInstance = new(material);

        Material[] materials = carRenderer.materials;
        materials[_mainColorMatId] = newMaterialInstance;
        carRenderer.materials = materials;
    }

    public void SetRandomColor()
    {
        Material color = randomColorPool[Random.Range(0, randomColorPool.Count)];
        SetColor(color);
    }

    private void Update()
    {
        if (!_engineStarting)
        {
            _engineVolume = Mathf.Lerp(_engineVolume, engineOn ? MaxEngineVolume : 0f, Time.deltaTime * 3f);
        }
        _audioSource.volume = _engineVolume;
        
        float steering = smoothSteering.Evaluate(Mathf.Abs(this.steering)) * steeringMaxAngle * Mathf.Sign(this.steering) +
                         _driftCounterSteering;

        _tireFr.localRotation = Quaternion.Euler(0f, steering, 0f);
        _tireFl.localRotation = Quaternion.Euler(0f, steering, 0f);
    }

    private void FixedUpdate()
    {
        _breakLight = false;
        _reverseLight = false;
        
        HandleCarPhysics();
        HandleEngineSound();
        HandleDrift();
        HandleLights();
        HandleReset();

        rbVelocity = _rb.linearVelocity;
    }

    private void HandleCarPhysics()
    {
        _torqueWheelContact = false;
        wheelContact = false;
        
        Vector3 forward = transform.forward;
            
        Vector3 forwardFlat = new Vector3(forward.x, 0, forward.z).normalized;
            
        _carAngle = Vector3.Angle(transform.forward, forwardFlat);
        
        // Process gears

        HandleGears();

        // Process wheels
        
        ProcessWheel(_tireFr, _wheel_fr, drivetrain is Drivetrain.FWD or Drivetrain.AWD, false);
        ProcessWheel(_tireFl, _wheel_fl, drivetrain is Drivetrain.FWD or Drivetrain.AWD, false);

        ProcessWheel(_tireRr, _wheel_rr, drivetrain is Drivetrain.RWD or Drivetrain.AWD, true);
        ProcessWheel(_tireRl, _wheel_rl, drivetrain is Drivetrain.RWD or Drivetrain.AWD, true);
        
        // Downforce
        
        if (true)
        {
            float angle = Vector3.Angle(transform.up, Vector3.up);

            Vector3 direction = angle < 45f ? -transform.up : -Vector3.up;
            
            _rb.AddForce(direction * (downForce * downforceCurve.Evaluate(relativeSpeed) * (1f - _carAngle / 90f)));
        }
        
        // Drift counter steering

        HandleCounterSteering();
        
        // Rear free torque

        if (!_wheel_rl.surfaceContact && !_wheel_rr.surfaceContact && accelInput != 0f)
        {
            _wheel_rr.SetRotationSpeed(20f * accelInput);
            _wheel_rl.SetRotationSpeed(20f * accelInput);
        }
    }

    private void ProcessWheel(Transform tire, Wheel wheel, bool applyTorque, bool isRear)
    {
        Ray ray = new()
        {
            origin = tire.position + tire.up * 0.5f,
            direction = -tire.up
        };

        float castOffset = wheelOffset;
        
        bool hit = Physics.SphereCast(ray, castOffset, out RaycastHit wheelHit, 
            suspensionLength + 0.5f - castOffset, layerMask);

        if (Mathf.Abs(Vector3.Dot(wheelHit.point - tire.position, tire.right)) > 0.2f)
        {
            hit = Physics.Raycast(ray, out wheelHit, suspensionLength + 0.5f, layerMask);
            castOffset = 0f;
        }

        carSpeed = Vector3.Dot(transform.forward, _rb.linearVelocity);
        speed = Mathf.Abs(carSpeed);
        float normalizedSpeed = Mathf.Clamp01(speed / topSpeed);
        float normalizedReverse = Mathf.Clamp01(speed / topReverse);
        relativeSpeed = normalizedSpeed;

        wheel.surfaceContact = hit;
        
        if (hit)
        {
            wheel.surfaceLayer = wheelHit.transform.gameObject.layer;
            
            // Grip calculation
            
            Vector3 wheelVelocity = _rb.GetPointVelocity(tire.position);
            
            float sign = Mathf.Sign(carSpeed);
            
            float slipAngle = Vector3.SignedAngle(tire.forward * sign, wheelVelocity, tire.up);
            slipAngle = Mathf.Deg2Rad * Mathf.Abs(slipAngle);
            if (isRear)
            {
                _rearSlipAngle = slipAngle;
            }
            slipAngle = Mathf.Clamp01(slipAngle);

            if (speed < 0.5f) slipAngle = 0f;

            float tireGrip = isRear ? rearTireGrip : frontTireGrip;
            if (isDriftCar && isRear && (accelInput > 0f || (burnout && speed < 2f))) tireGrip *= 0.6f;
            
            float grip = tireGrip;
            grip *= Mathf.Clamp(gripSpeedCurve.Evaluate(relativeSpeed) * gripSlipCurve.Evaluate(slipAngle), 0.5f, 1f);

            float surfaceGrip = wheel.surfaceLayer switch
            {
                7 => _trackGrip,
                10 => _trackGrip,
                _ => _otherGrip
            };
            
            bool emitTrail = ((slipAngle > driftTrailTrigger || (isRear && handbrake)) && speed > 1f)
                             || (engineOn && applyTorque && Mathf.Abs(accelInput) > 0.5f && speed < topSpeed * 0.1f)
                             || (burnout && speed < 2f && engineOn && applyTorque)
                             || (wheel.surfaceLayer != 7 && accelInput != 0 && applyTorque);

            wheel.SetTrailState(emitTrail, Mathf.Abs(Vector3.Dot(wheelVelocity, tire.right)));

            if (applyTorque) _torqueWheelContact = true;
            wheelContact = true;

            wheel.isContactingTrack = wheelHit.transform.gameObject.layer is 7 or 10;
            
            // Suspension
            
            Vector3 springDir = tire.up;

            float offset = suspensionRest - (wheelHit.distance - 0.5f + castOffset);
            float velocity = Vector3.Dot(springDir, wheelVelocity);
            float force = offset * springStrength - velocity * springDamper;

            if (force < 0f) force = 0f;
            _rb.AddForceAtPosition(springDir * force, tire.position);

            wheel.transform.position = tire.position + springDir * (-(wheelHit.distance - 0.5f + castOffset) + wheelOffset);
            
            // Acceleration

            Vector3 accelerationDir = Vector3.ProjectOnPlane(tire.forward, wheelHit.normal);

            float movingDir = Mathf.Sign(carSpeed);
            bool accelerate = movingDir == Mathf.Sign(accelInput);
            float curveValue = accelInput < 0f ? normalizedReverse : normalizedSpeed;
            float speedFactor = accelerate ? accelerationCurve.Evaluate(curveValue) : 0f;
            float availableTorque = torque * accelInput * speedFactor;

            availableTorque *= useGripInAcceleration ? grip : surfaceGrip;
            
            if (drivetrain == Drivetrain.AWD) availableTorque *= 0.5f;
            if (speed < topSpeed * 0.1f && Mathf.Abs(accelInput) > 0.5f) availableTorque *= 0.5f;
            
            if (applyTorque && (!isRear || !handbrake) && engineOn)
            {
                _rb.AddForceAtPosition(accelerationDir * availableTorque, wheel.transform.position);
                if (availableTorque < 0f) _reverseLight = true;
            }
            
            float accelerationVelocity = Vector3.Dot(tire.forward, wheelVelocity);

            float rotationSpeed = isRear && accelInput != 0f
                ? wheelVelocity.magnitude * movingDir
                : accelerationVelocity;

            if (((accelInput > 0.5f && speed < topSpeed * 0.1f) || (burnout && speed < 2f)) && engineOn && applyTorque) rotationSpeed = 10f;
            
            wheel.SetRotationSpeed(rotationSpeed);
            
            // Breaks

            bool isBreaking = !accelerate && accelInput != 0f;
            
            if (isBreaking && (!isRear || !handbrake) && !(handbrake && speed < 0.5f))
            {
                _rb.AddForceAtPosition(accelerationDir * (breakForce * Mathf.Sign(accelInput) * grip), wheel.transform.position);
                _breakLight = true;
            }
            else
            {
                isBreaking = false;
            }
            
            // Handbrake

            if (isRear && handbrake)
            {
                if (!isBreaking)
                {
                    Vector3 direction = _rb.GetPointVelocity(wheel.transform.position).normalized;
                    _rb.AddForceAtPosition(-direction * handbrakeForce, wheel.transform.position);
                    _breakLight = true;
                }

                grip *= handbrakeGripMultiplier;
            }
            
            // Drag
            
            float drag = Mathf.Abs(carSpeed);
            if (drag > 0.5f)
            {
                drag = 1f;
                drag *= Mathf.Sign(carSpeed);
                drag *= 0.5f;
            }
            else if (accelInput == 0f && _rb.linearVelocity.magnitude < 0.5f && _carAngle < 5f)
            {
                drag = accelerationVelocity * _rb.mass * 0.25f / Time.fixedDeltaTime;
            }
            _rb.AddForceAtPosition(-accelerationDir * drag, wheel.transform.position);
            
            // Steering

            Vector3 steeringDir = Vector3.ProjectOnPlane(tire.right, wheelHit.normal);

            float steeringVelocity = Vector3.Dot(steeringDir, wheelVelocity);
            float desiredVelocityChange = -steeringVelocity * grip * surfaceGrip;
            float absSteeringVelocity = Mathf.Abs(steeringVelocity);

            float absoluteGripVelocity = isRear ? 0.3f : 0.5f;
            if (wheel.surfaceLayer != 7) absoluteGripVelocity = -1f;
            
            if (absSteeringVelocity < absoluteGripVelocity) desiredVelocityChange = -steeringVelocity;
            float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
            
            float mass = tireMass;
            float massInterpolation = absSteeringVelocity / absoluteGripVelocity;
            massInterpolation *= massInterpolation;
            if (absSteeringVelocity < absoluteGripVelocity) mass = Mathf.Lerp(_rb.mass * 0.25f, mass, massInterpolation);
            _rb.AddForceAtPosition(steeringDir * (mass * desiredAcceleration), wheel.transform.position);
        }
        else
        {
            wheel.transform.position = tire.position + tire.up * (-suspensionLength + wheelOffset);

            wheel.SetTrailState(false, 0f);
            wheel.isContactingTrack = false;
        }
    }

    private float _counterSteeringPower = 1f;
    private void HandleCounterSteering()
    {
        float angle = Vector3.SignedAngle(transform.forward, _rb.linearVelocity, Vector3.up);
        if (isBot) angle = Mathf.Clamp(angle, -30f, 30f);
        if (Mathf.Abs(angle) < 90f && speed > 1f && wheelContact)
        {
            const float expoTrigger = 1.5f;
                
            float angleRatio = angle * (Mathf.Deg2Rad * expoTrigger);
            if (angleRatio > 1f && !isBot) angleRatio *= Mathf.Abs(angleRatio);

            angleRatio *= 1f / expoTrigger;

            bool underSteering = Mathf.Sign(steering) != Mathf.Sign(angle) && steering != 0f && accelInput < 1f;
            _counterSteeringPower = Mathf.Lerp(_counterSteeringPower, underSteering ? 0.7f : 1f, Time.fixedDeltaTime * 10f);
            
            angleRatio *= _counterSteeringPower;
            
            _driftCounterSteering = angleRatio * (isBot ? botCounterSteering : driftCounterSteering);
        }
        else
        {
            _driftCounterSteering = Mathf.Lerp(_driftCounterSteering, 0f, Time.fixedDeltaTime * 5f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_wheel_fl) return;
        
        Wheel[] wheels = { _wheel_fl, _wheel_fr, _wheel_rl, _wheel_rr };
        
        Gizmos.color = Color.white;
        foreach (Wheel wheel in wheels)
        {
            Gizmos.DrawSphere(wheel.transform.position, wheelOffset);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_rb.worldCenterOfMass, 0.1f);
    }

    private float _enginePitchFactor = 0f;
    private float _engineVolume = 0f;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void HandleEngineSound()
    {
        if (!_audioSource) return;
        if (!engineOn) return;
        
        bool accelerate = Mathf.Sign(carSpeed) == Mathf.Sign(accelInput) && accelInput != 0f;

        float bottomBoundary = relativeGears[_currentGear - 1];
        float topBoundary = _currentGear < relativeGears.Count ? relativeGears[_currentGear] : 1f;
        float gearPitch = (relativeSpeed - bottomBoundary) / (topBoundary - bottomBoundary);
        
        float to = accelerate 
            ? gearPitch + (drivetrain == Drivetrain.FWD || !wheelContact ? 0f : _rearSlipAngle) 
            : gearPitch - 0.1f;

        if ((Mathf.Abs(accelInput) > 0.5f && speed < topSpeed * 0.1f) || (burnout && speed < 2f))
        {
            to = maxEnginePitch;
        }

        if (accelInput != 0f && !_torqueWheelContact) to = maxEnginePitch;

        float lerpSpeed = _enginePitchFactor < to || !wheelContact ? 1f : 5f;
        _enginePitchFactor = Mathf.Lerp(_enginePitchFactor, to, Time.deltaTime * lerpSpeed);

        _enginePitchFactor = Mathf.Clamp01(_enginePitchFactor);

        float minPitch = gearMinPitch[_currentGear - 1];
        _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - minPitch) + minPitch;

        _audioSource.volume = _engineVolume;
    }

    private void HandleGears()
    {
        if (!engineOn) return;
        
        if (relativeGears[_currentGear - 1] > relativeSpeed)
        {
            ChangeGear(-1);
        }
        
        if (_currentGear >= relativeGears.Count) return;
        
        if (relativeGears[_currentGear] < relativeSpeed)
        {
            ChangeGear(1);
        }
    }

    private void ChangeGear(int delta)
    {
        _currentGear += delta;
        
        if (isBot) return;
        
        _gearShiftSource.clip = gearShiftClip;
        _gearShiftSource.pitch = Random.Range(0.8f, 1.2f);
        _gearShiftSource.Play();
    }

    private void HandleDrift()
    {
        if (!isDriftCar) return;
        if (!wheelContact) return;
        if (!_driftManager) return;

        int tiresOnTrack = 0;

        if (_wheel_fl.isContactingTrack) tiresOnTrack++;
        if (_wheel_fr.isContactingTrack) tiresOnTrack++;
        if (_wheel_rl.isContactingTrack) tiresOnTrack++;
        if (_wheel_rr.isContactingTrack) tiresOnTrack++;
        
        if (tiresOnTrack < 3) return;
        
        _driftManager.ProcessDrift(_rb.linearVelocity.magnitude, _rearSlipAngle);
    }

    private void HandleLights()
    {
        if (speed < 1f) _breakLight = true;
        
        _breakLight = _breakLight && (engineOn || _engineStarting);
        _reverseLight = _reverseLight && (engineOn || _engineStarting);
        
        _breakLightMat?.SetColor(EmissionColor, _breakLight ? _breakEmissionColor : Color.black);
        _breakFlareMat?.SetColor(EmissionColor, _breakLight ? _breakEmissionColor : _breakFlareEmissionColor);
        _reverseLightMat?.SetColor(EmissionColor, _reverseLight ? _reverseEmissionColor : Color.black);
    }

    [HideInInspector] public bool botReset = true;
    [HideInInspector] public float stuckTimer = 0f;
    private void HandleReset()
    {
        if (!_levelManager) return;

        bool isFreeRoam = _levelManager.raceMode == RaceMode.FreeRoam;
        
        Wheel[] wheels = { _wheel_fl, _wheel_fr, _wheel_rl, _wheel_rr };
        
        int tiresOnSurface = wheels.Count(wheel => isFreeRoam ? wheel.surfaceContact : wheel.isContactingTrack);

        if (engineOn && (_rb.linearVelocity.magnitude < 2f || tiresOnSurface < 4 || _levelManager.wrongDirection))
        {
            stuckTimer += Time.fixedDeltaTime * (_levelManager.wrongDirection ? 1.5f : 1f);

            float targetTime = 2f;
            
            if (stuckTimer > targetTime)
            {
                if (!isBot)
                {
                    _levelManager.ResetCar(true);
                }
                else
                {
                    botReset = true;
                }
            }
        }
        else if (!_levelManager.wrongDirectionActive)
        {
            stuckTimer = 0f;
            if (!isBot)
            {
                _levelManager.ResetCar(false);
            }
            else
            {
                botReset = false;
            }
        }
        
        if (!_pendingReset) return;

        _pendingReset = false;
        
        if (!_levelManager.resetCar && !_forceReset) return;

        _forceReset = false;
        _levelManager.ResetCar(false);
        stuckTimer = 0f;

        if (_levelManager.raceMode == RaceMode.FreeRoam)
        {
            ResetToNearestTrackNode();
        }
        else
        {
            ResetToLastCheckpoint();   
        }
    }

    private void ResetToLastCheckpoint()
    {
        Ray ray = new()
        {
            origin = _levelManager.lastCheckPoint.transform.position + Vector3.up * 10f,
            direction = Vector3.down
        };

        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit,  20f, layerMask, QueryTriggerInteraction.Ignore);

        if (hit)
        {
            _rb.MovePosition(raycastHit.point);
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            Quaternion dirRot = Quaternion.LookRotation(
                _levelManager.lastCheckPoint.GetNext().transform.position - _levelManager.lastCheckPoint.transform.position,
                Vector3.up
            );

            Quaternion checkpointRot = Quaternion.LookRotation(
                _levelManager.lastCheckPoint.GetForward(),
                Vector3.up
            );
            
            Quaternion flatInterpolatedRot = Quaternion.Slerp(dirRot, checkpointRot, 0.5f);
            
            Quaternion alignedToNormal = Quaternion.FromToRotation(Vector3.up, raycastHit.normal);
            
            transform.rotation = alignedToNormal * flatInterpolatedRot;
            
            _rb.linearVelocity = transform.forward * 20f;
            
            _levelManager.SnapCamera();
        }
    }

    private void ResetToNearestTrackNode()
    {
        RacingLine racingLine = FindFirstObjectByType<RacingLine>();

        int nearestNode = racingLine.GetNearestNodeID(transform.position);
        int nextNode = racingLine.ForecastRacingNode(nearestNode, 1);
        
        Quaternion rot = Quaternion.LookRotation(
            racingLine.orderedNodes[nextNode].position -
            racingLine.orderedNodes[nearestNode].position, Vector3.up);
        ResetToPosition(racingLine.orderedNodes[nearestNode].position, rot);
        
        _levelManager.SnapCamera();
    }

    public IEnumerator StartEngine(float delay = 0f)
    {
        if (engineOn) yield break;
        if (_engineStarting) yield break;
        
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        
        if (_nightMode)
        {
            GraphicsSmoke headlightsQuality = Settings.Get().headlightsQuality;
            bool allowHeadlight = headlightsQuality == GraphicsSmoke.All ||
                                  (headlightsQuality == GraphicsSmoke.Player && (!isBot || menuMode));
            
            _frontLightSource?.SetActive(allowHeadlight);
            _frontLightMat?.SetColor(EmissionColor, _frontEmissionColor);
            _breakFlareEmissionColor = _redLightEmissionColor;
        }
        
        _engineStarting = true;
        
        _gearShiftSource.clip = engineStartClip;
        _gearShiftSource.pitch = Random.Range(0.95f, 1.05f);
        _gearShiftSource.Play();

        yield return new WaitForSeconds(0.4f);

        _enginePitchFactor = 0f;
        _audioSource.pitch = minEnginePitch;

        while (_engineVolume < MaxEngineVolume * 0.6f)
        {
            _engineVolume = Mathf.Lerp(_engineVolume, MaxEngineVolume, Time.deltaTime * 3f);

            yield return null;
        }

        while (_enginePitchFactor < 0.3f)
        {
            _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - minEnginePitch) +
                                 minEnginePitch;
            _engineVolume = Mathf.Lerp(_engineVolume, MaxEngineVolume, Time.deltaTime * 3f);

            _enginePitchFactor += Time.deltaTime * 0.5f;

            yield return null;
        }
        
        while (_enginePitchFactor > 0f)
        {
            _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - minEnginePitch) + minEnginePitch;
            _engineVolume = Mathf.Lerp(_engineVolume, MaxEngineVolume, Time.deltaTime * 3f);

            _enginePitchFactor -= Time.deltaTime * 0.3f;
            _enginePitchFactor = Mathf.Clamp01(_enginePitchFactor);

            yield return null;
        }

        engineOn = true;
        _engineStarting = false;
        
        _levelManager?.OnCarStarted();
    }

    private void StartEngineImmediate()
    {
        if (_nightMode)
        {
            _frontLightSource?.SetActive(true);
            _frontLightMat?.SetColor(EmissionColor, _frontEmissionColor);
            _breakFlareEmissionColor = _redLightEmissionColor;
        }

        engineOn = true;
    }

    public IEnumerator StopEngine()
    {
        if (!engineOn) yield break;
        if (_engineStarting) yield break;
        
        if (_nightMode)
        {
            _frontLightSource?.SetActive(false);
            _frontLightMat?.SetColor(EmissionColor, Color.black);
            _breakFlareEmissionColor = Color.black;
        }
        
        _engineStarting = true;
        engineOn = false;
        
        _audioSource.pitch = minEnginePitch;

        while (_enginePitchFactor > 0f)
        {
            _audioSource.pitch = enginePitchCurve.Evaluate(_enginePitchFactor) * (maxEnginePitch - minEnginePitch) +
                                 minEnginePitch;

            _enginePitchFactor -= Time.deltaTime * 0.5f;

            yield return null;
        }
        
        _gearShiftSource.clip = engineStopClip;
        _gearShiftSource.pitch = 1f;
        _gearShiftSource.Play();

        _engineStarting = false;
    }

    public void SetMenuMode(bool carSelect = false)
    {
        menuMode = true;
        _nightMode = true;
            
        if (!carSelect)
        {
            this.steering = -0.5f;
            accelInput = 1f;
            float steering = smoothSteering.Evaluate(Mathf.Abs(this.steering)) * steeringMaxAngle * Mathf.Sign(this.steering) +
                             _driftCounterSteering;
            
            _tireFr.localRotation = Quaternion.Euler(0f, steering, 0f);
            _tireFl.localRotation = Quaternion.Euler(0f, steering, 0f);
            
            StartEngineImmediate();
        }
        else
        {
            StartCoroutine(StartEngine());
            _frontLightSource?.SetActive(false);
        }
    }

    public void SetShowcaseMode()
    {
        _frontLightMat?.SetColor(EmissionColor, _frontEmissionColor);
        _breakFlareEmissionColor = _redLightEmissionColor;
    }

    public void InvokeReset(bool force)
    {
        _pendingReset = true;
        _forceReset = force;
    }

    public void ResetToPosition(Vector3 pos, Quaternion rot)
    {
        Ray ray = new()
        {
            origin = pos + Vector3.up * 10f,
            direction = Vector3.down
        };

        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit,  Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore);

        if (hit)
        {
            _rb.MovePosition(raycastHit.point + Vector3.up * 0.1f);
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            
            Vector3 rotation = rot.eulerAngles;
            rotation.x = 0f;
            rotation.z = 0f;

            transform.rotation = Quaternion.Euler(rotation);
        }
    }

    public void StopCar()
    {
        forceStop = true;

        accelInput = 0f;
        steering = 0f;
        handbrake = true;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (isDriftCar)
        {
            if (other.impulse.magnitude > 0.5f && other.gameObject.layer != 7)
            {
                _driftManager.OnDriftFail();
            }
        }
    }
}
