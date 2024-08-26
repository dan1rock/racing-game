using System;
using Unity.Collections;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] private Transform tire_fr;
    [SerializeField] private Transform tire_fl;
    [SerializeField] private Transform tire_rr;
    [SerializeField] private Transform tire_rl;
    
    [Header("Suspension")] 
    [SerializeField] private float suspensionRest = 0.5f;
    [SerializeField] private float springStrength = 30f;
    [SerializeField] private float springDamper = 10f;

    [Header("Steering")] 
    [SerializeField] private float tireGrip = 0.8f;
    [SerializeField] private float tireMass = 1f;
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private AnimationCurve gripCurve;

    [Header("Acceleration")] 
    [SerializeField] private float torque = 1f;
    [SerializeField] private float topSpeed = 2f;
    
    [SerializeField] private LayerMask layerMask;

    [SerializeField] [ReadOnly] private float speed;
    [SerializeField] [ReadOnly] private float relativeSpeed;

    private float _acceleration = 0f;
        
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
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
        ProcessWheel(tire_fr);
        ProcessWheel(tire_fl);
        ProcessWheel(tire_rr);
        ProcessWheel(tire_rl);
    }

    private void ProcessWheel(Transform wheel)
    {
        Ray ray = new()
        {
            origin = wheel.position + wheel.up * 0.5f,
            direction = -wheel.up
        };
        bool hit = Physics.Raycast(ray, out RaycastHit wheelRay, 1f, layerMask);

        float carSpeed = Vector3.Dot(transform.forward, _rb.velocity);
        speed = Mathf.Abs(carSpeed);
        float normalizedSpeed = Mathf.Clamp01(speed / topSpeed);
        relativeSpeed = normalizedSpeed;
        
        if (hit)
        {
            // Suspension
            
            Vector3 springDir = wheel.up;
            Vector3 wheelVelocity = _rb.GetPointVelocity(wheel.position);

            float offset = suspensionRest - wheelRay.distance + 0.5f;
            float velocity = Vector3.Dot(springDir, wheelVelocity);
            float force = offset * springStrength - velocity * springDamper;
            
            _rb.AddForceAtPosition(springDir * force, wheel.position);
            
            // Steering

            Vector3 steeringDir = wheel.right;

            float steeringVelocity = Vector3.Dot(steeringDir, wheelVelocity);
            float desiredVelocityChange = -steeringVelocity * tireGrip * gripCurve.Evaluate(relativeSpeed);
            float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
            
            _rb.AddForceAtPosition(steeringDir * (tireMass * desiredAcceleration), wheel.position);
            
            // Acceleration

            Vector3 accelerationDir = wheel.forward;

            float speedFactor = Mathf.Sign(carSpeed) == Mathf.Sign(_acceleration) ? 1f - normalizedSpeed : 1f;
            float availableTorque = torque * _acceleration * speedFactor;
            
            _rb.AddForceAtPosition(accelerationDir * availableTorque, wheel.position);
            
            // Drag

            float drag = Mathf.Abs(carSpeed);
            if (drag > topSpeed * 0.1f) drag = topSpeed * 0.1f;
            drag *= Mathf.Sign(carSpeed);
            drag *= 0.2f;
            _rb.AddForceAtPosition(-accelerationDir * drag, wheel.position);
        }
    }
}
