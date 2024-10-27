using System;
using UnityEngine;

public class CarBot : MonoBehaviour
{
    private LayerMask _boundariesLayer = 1 << 9;

    private Car _car;
    private RacingLine _racingLine;
    private LevelManager _levelManager;

    private int _currentNodeId;
    private int _currentNodeTargetId;
    private Vector3 _currentNodeMarker;

    private void Awake()
    {
        _car = GetComponent<Car>();
        _racingLine = FindObjectOfType<RacingLine>();
        _levelManager = FindObjectOfType<LevelManager>();
    }

    private void Start()
    {
        _car.StartCoroutine(_car.StartEngine());

        _currentNodeId = _racingLine.GetNearestNodeID(transform.position);
        _currentNodeTargetId = _currentNodeId;
        _currentNodeMarker = _racingLine.orderedNodes[_currentNodeId].position;
    }

    private void FixedUpdate()
    {
        HandleRacingLine();
        HandleAcceleration();
        HandleSteering();
    }

    private void HandleRacingLine()
    {
        int nodesN = _racingLine.orderedNodes.Count;
        
        int prev = _currentNodeId - 1;
        if (prev < 0) prev = nodesN - 1;

        int next = _currentNodeId + 1;
        if (next >= nodesN) next = 0;

        if (_levelManager.reverse) (next, prev) = (prev, next);

        float prevDistance = (_racingLine.orderedNodes[prev].position - transform.position).magnitude;
        float currentDistance = (_racingLine.orderedNodes[_currentNodeId].position - transform.position).magnitude;

        if (currentDistance < prevDistance)
        {
            _currentNodeId = next;

            if (!_levelManager.reverse)
            {
                _currentNodeTargetId = _currentNodeId + 3;
                if (_currentNodeTargetId >= nodesN) _currentNodeTargetId -= nodesN;
            }
            else
            {
                _currentNodeTargetId = _currentNodeId - 3;
                if (_currentNodeTargetId < 0) _currentNodeTargetId += nodesN;
            }
        }
        
        _currentNodeMarker = Vector3.Lerp(
            _currentNodeMarker, 
            _racingLine.orderedNodes[_currentNodeTargetId].position, 
            1f
            );
    }

    private void HandleAcceleration()
    {
        Vector3 origin = transform.position + transform.forward;
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

        float maxDistance = 20f;
        float minDistance = 5f;
        float angle = 15f;
        
        Ray rayRight = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, angle, 0) * flatForward
        };
        
        angle = -angle;
        
        Ray rayLeft = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, angle, 0) * flatForward
        };
        
        _car.accelInput = 1f;
        
        bool hit = Physics.Raycast(rayRight, out RaycastHit raycastHit, maxDistance, _boundariesLayer);
        Debug.DrawLine(rayRight.origin, rayRight.origin + rayRight.direction * maxDistance, Color.red);
        
        if (hit)
        {
            _car.accelInput -= (1f - GetInterpolatedValue(raycastHit.distance, minDistance, maxDistance)) * 0f;
        }
        
        hit = Physics.Raycast(rayLeft, out raycastHit, maxDistance, _boundariesLayer);
        Debug.DrawLine(rayLeft.origin, rayLeft.origin + rayLeft.direction * maxDistance, Color.red);
        
        if (hit)
        {
            _car.accelInput -= (1f - GetInterpolatedValue(raycastHit.distance, minDistance, maxDistance)) * 0f;
        }
        
        Vector3 flatDirection = _currentNodeMarker - transform.position;
        flatDirection.y = 0f;
        float signedAngle = Vector3.SignedAngle(flatForward, flatDirection, Vector3.up) / 60f;
        signedAngle = Mathf.Clamp01(Mathf.Abs(signedAngle));
        signedAngle *= 0.8f;

        _car.accelInput -= signedAngle;
    }
    
    private void HandleSteering()
    {
        Vector3 origin = transform.position + transform.forward;

        float maxDistance = 10f;
        float minDistance = 1f;
        float angle = 30f;
        
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        
        Ray rayRight = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, angle, 0) * flatForward
        };
        
        Ray rayLeft = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, -angle, 0) * flatForward
        };
        
        Vector3 flatDirection = _currentNodeMarker - transform.position;
        flatDirection.y = 0f;
        
        float signedAngle = Vector3.SignedAngle(flatForward, flatDirection, Vector3.up) / 60f;
        signedAngle = Mathf.Clamp(signedAngle, -1f, 1f);

        float steering = 0f;
        steering += ProcessSteeringRay(rayRight, maxDistance, minDistance, maxDistance, -0.1f);
        steering += ProcessSteeringRay(rayLeft, maxDistance, minDistance , maxDistance, 0.1f);
        steering += signedAngle * 0.3f;

        if (_car.carSpeed < 0f) steering = -steering;

        bool retract = (_car.steering > 0f && steering < _car.steering)
                       || (_car.steering < 0f && steering > _car.steering);
        _car.steering = Mathf.Lerp(_car.steering, steering, retract ? 0.7f : 0.5f);
    }

    private float ProcessSteeringRay(Ray ray, float dist, float minDist, float maxDist, float ratio)
    {
        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, dist, _boundariesLayer);
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * dist, Color.red);
        
        if (hit)
        {
            return (1f - GetInterpolatedValue(raycastHit.distance, minDist, maxDist)) * ratio;
        }

        return 0f;
    }
    
    private float GetInterpolatedValue(float value, float min, float max)
    {
        if (value <= min)
            return 0f;

        if (value >= max)
            return 1f;
        
        return (value - min) / (max - min);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_currentNodeMarker, 2f);
    }

    [ContextMenu("Reset Car")]
    public void Reset()
    {
        _car.InvokeReset(true);
    }
}
