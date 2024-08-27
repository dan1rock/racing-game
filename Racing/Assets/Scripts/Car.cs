using System;
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

    [Header("Steering")] 
    [SerializeField] private float tireGrip = 0.8f;
    [SerializeField] private float tireMass = 1f;
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private AnimationCurve gripCurve;

    [Header("Acceleration")] 
    [SerializeField] private Drivetrain drivetrain;
    [SerializeField] private float torque = 1f;
    [SerializeField] private float topSpeed = 2f;
    [SerializeField] private AnimationCurve accelerationCurve;
    
    [SerializeField] private LayerMask layerMask;

    [SerializeField] [ReadOnly] private float speed;
    [SerializeField] [ReadOnly] private float relativeSpeed;

    private float _acceleration = 0f;
        
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _wheel_fr = wheel_fr.GetComponent<Wheel>();
        _wheel_fl = wheel_fl.GetComponent<Wheel>();
        _wheel_rr = wheel_rr.GetComponent<Wheel>();
        _wheel_rl = wheel_rl.GetComponent<Wheel>();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        _acceleration = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            _acceleration += 1f;
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            _acceleration -= 1f;
        }

        float steering = 0f;
        
        if (Input.GetKey(KeyCode.A))
        {
            steering -= 25f;
        }
        
        if (Input.GetKey(KeyCode.D))
        {
            steering += 25f;
        }

        steering *= steeringCurve.Evaluate(relativeSpeed);
        
        tire_fr.localRotation = Quaternion.Euler(0f, steering, 0f);
        tire_fl.localRotation = Quaternion.Euler(0f, steering, 0f);
    }

    private void FixedUpdate()
    {
        HandleCarPhysics();
    }

    private void HandleCarPhysics()
    {
        ProcessWheel(tire_fr, _wheel_fr, drivetrain is Drivetrain.FWD or Drivetrain.AWD);
        ProcessWheel(tire_fl, _wheel_fl, drivetrain is Drivetrain.FWD or Drivetrain.AWD);

        ProcessWheel(tire_rr, _wheel_rr, drivetrain is Drivetrain.RWD or Drivetrain.AWD);
        ProcessWheel(tire_rl, _wheel_rl, drivetrain is Drivetrain.RWD or Drivetrain.AWD);
    }

    private void ProcessWheel(Transform tire, Wheel wheel, bool applyTorque)
    {
        Ray ray = new()
        {
            origin = tire.position + tire.up * 0.5f,
            direction = -tire.up
        };
        bool hit = Physics.Raycast(ray, out RaycastHit wheelRay, suspensionLength + 0.5f, layerMask);

        float carSpeed = Vector3.Dot(transform.forward, _rb.velocity);
        speed = Mathf.Abs(carSpeed);
        float normalizedSpeed = Mathf.Clamp01(speed / topSpeed);
        relativeSpeed = normalizedSpeed;
        
        if (hit)
        {
            // Suspension
            
            Vector3 springDir = tire.up;
            Vector3 wheelVelocity = _rb.GetPointVelocity(tire.position);

            float offset = suspensionRest - wheelRay.distance + 0.5f;
            float velocity = Vector3.Dot(springDir, wheelVelocity);
            float force = offset * springStrength - velocity * springDamper;
            
            _rb.AddForceAtPosition(springDir * force, tire.position);

            wheel.transform.position = tire.position + springDir * (offset + wheelOffset);
            
            // Steering

            Vector3 steeringDir = tire.right;

            float steeringVelocity = Vector3.Dot(steeringDir, wheelVelocity);
            float desiredVelocityChange = -steeringVelocity * tireGrip * gripCurve.Evaluate(relativeSpeed);
            float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
            
            _rb.AddForceAtPosition(steeringDir * (tireMass * desiredAcceleration), tire.position);
            
            // Acceleration

            Vector3 accelerationDir = tire.forward;
            
            float speedFactor = Mathf.Sign(carSpeed) == Mathf.Sign(_acceleration) ? accelerationCurve.Evaluate(normalizedSpeed) : 1f;
            float availableTorque = torque * _acceleration * speedFactor;
            if (drivetrain == Drivetrain.AWD) availableTorque *= 0.5f;
            
            if (applyTorque)
            {
                _rb.AddForceAtPosition(accelerationDir * availableTorque, tire.position);
            }
            
            float accelerationVelocity = Vector3.Dot(accelerationDir, wheelVelocity);
            wheel.SetRotationSpeed(accelerationVelocity);
            
            // Drag

            float drag = Mathf.Abs(carSpeed);
            if (drag > topSpeed * 0.1f) drag = topSpeed * 0.1f;
            drag *= Mathf.Sign(carSpeed);
            drag *= 0.5f;
            _rb.AddForceAtPosition(-accelerationDir * drag, tire.position);
        }
    }
}
