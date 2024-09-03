using System;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    [SerializeField] private float velocityRatio = 50f;

    private WheelTrail _wheelTrail;

    private Transform _wheelPhysics;
    private Transform _wheelHolder;
    
    private float _currentRotationSpeed = 0f;
    private float _velocityUpdated = 0f;

    private void Awake()
    {
        _wheelHolder = transform.parent;
        _wheelPhysics = _wheelHolder.parent;
        _wheelTrail = _wheelHolder.GetComponentInChildren<WheelTrail>();
    }

    private void Update()
    {
        Vector3 newEuler = _wheelHolder.localEulerAngles;
        newEuler.y = 0f;
        float wheelRotation = _wheelPhysics.localEulerAngles.y;
        if (wheelRotation > 180f) wheelRotation -= 360f;
        if (wheelRotation > 1f)
        {
            Debug.Log(wheelRotation);
        }
        if (Mathf.Abs(wheelRotation) > 45f)
        {
            newEuler.y = -wheelRotation + 45f * Mathf.Sign(wheelRotation);
        }
        _wheelHolder.localRotation = Quaternion.Euler(newEuler);

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
