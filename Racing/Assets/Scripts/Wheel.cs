using System;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    [SerializeField] private float velocityRatio = 50f;

    private WheelTrail _wheelTrail;
    
    private float _currentRotationSpeed = 0f;
    private float _velocityUpdated = 0f;

    private void Awake()
    {
        _wheelTrail = transform.parent.GetComponentInChildren<WheelTrail>();
    }

    private void Update()
    {
        transform.Rotate(Vector3.right, _currentRotationSpeed * Time.deltaTime);

        if (Time.time - _velocityUpdated < 1f) return;
        
        _currentRotationSpeed -= _currentRotationSpeed * 0.3f * Time.deltaTime;
    }

    public void SetRotationSpeed(float speed)
    {
        _currentRotationSpeed = speed * velocityRatio;
        _velocityUpdated = Time.time;
    }

    public void SetTrailState(bool state, float speed)
    {
        if (!_wheelTrail) return;

        _wheelTrail.emitTrail = state;
        _wheelTrail.wheelSpeed = speed;
    }
}
