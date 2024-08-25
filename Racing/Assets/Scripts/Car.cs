using System;
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
    
    [SerializeField] private LayerMask layerMask;
    
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
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
            origin = wheel.position,
            direction = -wheel.up
        };
        bool hit = Physics.Raycast(ray, out RaycastHit wheelRay, .5f, layerMask);

        if (hit)
        {
            Vector3 springDir = wheel.up;
            Vector3 wheelVelocity = _rb.GetPointVelocity(wheel.position);

            float offset = suspensionRest - wheelRay.distance;
            float velocity = Vector3.Dot(springDir, wheelVelocity);
            float force = offset * springStrength - velocity * springDamper;
            
            _rb.AddForceAtPosition(springDir * force, wheel.position);
        }
    }
}
