using System;
using UnityEngine;

public class CarBot : CarController
{
    public float speedLimit = Mathf.Infinity;
    public bool dontBreak = false;
    public float maxAcceleration = 1f;
    public float steeringReaction = 1f;

    public float fallBehindAdjustment = 1f;
    public float fallAheadAdjustment = 1f;

    private float _directionAngle;
    private float _launchTime;
    
    private LayerMask _boundariesLayer = 1 << 9;
    private LayerMask _carLayer = 1 << 6;

    private Collider _selfCollider;
    
    private int _currentNodeTargetId;
    private Vector3 _currentNodeMarker;

    private bool _isActive = false;
    private bool _stopCar = false;

    protected override void Initialize()
    {
        base.Initialize();
        
        car = GetComponent<Car>();
        _selfCollider = GetComponentInChildren<Collider>();

        car.isBot = true;
    }

    private void Start()
    {
        currentNodeId = racingLine.GetNearestNodeID(transform.position);
        _currentNodeTargetId = currentNodeId;
        _currentNodeMarker = racingLine.orderedNodes[currentNodeId].position;

        if (ForecastRacingNode(-1) == racingLine.startNodeId) currentLap += 1;
        
        if (levelManager.botCar) ActivateBot();
    }

    private void Update()
    {
        HandleLeaderboardName();
    }

    private void FixedUpdate()
    {
        HandleRacingLine();
        CalculateTotalDistance();
        
        if (!_isActive) return;
        
        HandleAcceleration();
        HandleSteering();
        HandleReset();

        if (currentLap > levelManager.laps && !_stopCar)
        {
            car.StopCar();
            _stopCar = true;
        }
    }

    private void HandleRacingLine()
    {
        int nodesN = racingLine.orderedNodes.Count;
        
        int prev = currentNodeId - 1;
        if (prev < 0) prev = nodesN - 1;

        int next = currentNodeId + 1;
        if (next >= nodesN) next = 0;

        if (levelManager.reverse) (next, prev) = (prev, next);

        float prevDistance = (racingLine.orderedNodes[prev].position - transform.position).magnitude;
        float currentDistance = (racingLine.orderedNodes[currentNodeId].position - transform.position).magnitude;

        if (currentDistance < prevDistance)
        {
            if (currentNodeId == racingLine.startNodeId) currentLap += 1;
            
            currentNodeId = next;
            
            _currentNodeTargetId = ForecastRacingNode(3);
        }
        
        _currentNodeMarker = Vector3.Lerp(
            _currentNodeMarker, 
            racingLine.orderedNodes[_currentNodeTargetId].position, 
            1f
            );
    }

    private void HandleAcceleration()
    {
        if (_stopCar) return;
        
        Vector3 origin = transform.position + transform.forward + Vector3.up * 0.5f;
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
        
        car.accelInput = maxAcceleration;

        bool maxThrottle = Time.time - _launchTime < 3f;
        if (maxThrottle) car.accelInput = 1f;

        if (levelManager.player)
        {
            float dist = levelManager.player.GetPlayerDistance() - totalDistance;

            dist *= dist < 0f ? fallAheadAdjustment : fallBehindAdjustment;
            
            car.accelInput += dist / 200f;
            car.accelInput = Mathf.Clamp(car.accelInput, 0.4f, 1.5f);

            if (maxThrottle) car.accelInput = Mathf.Max(1f, car.accelInput);
        }
        
        bool hit = Physics.Raycast(rayRight, out RaycastHit raycastHit, maxDistance, _boundariesLayer);
        Debug.DrawLine(rayRight.origin, rayRight.origin + rayRight.direction * maxDistance, Color.red);
        
        if (hit)
        {
            car.accelInput -= (1f - GetInterpolatedValue(raycastHit.distance, minDistance, maxDistance)) * 0f;
        }
        
        hit = Physics.Raycast(rayLeft, out raycastHit, maxDistance, _boundariesLayer);
        Debug.DrawLine(rayLeft.origin, rayLeft.origin + rayLeft.direction * maxDistance, Color.red);
        
        if (hit)
        {
            car.accelInput -= (1f - GetInterpolatedValue(raycastHit.distance, minDistance, maxDistance)) * 0f;
        }
        
        Vector3 flatDirection = _currentNodeMarker - transform.position;
        flatDirection.y = 0f;
        float signedAngle = Vector3.SignedAngle(flatForward, flatDirection, Vector3.up);
        _directionAngle = signedAngle;
        float deceleration = Mathf.Clamp01(Mathf.Abs(signedAngle / 60f));
        deceleration *= 0.8f;

        if (!maxThrottle)
        {
            car.accelInput -= deceleration * car.accelInput;
        }

        Vector3 breakForecast = racingLine.orderedNodes[ForecastRacingNode(3 + racingLine.breakForecast)].position -
                                racingLine.orderedNodes[ForecastRacingNode(2 + racingLine.breakForecast)].position;
        Vector3 currentLine = racingLine.orderedNodes[ForecastRacingNode(2)].position -
                              racingLine.orderedNodes[ForecastRacingNode(1)].position;
        float forecastAngle = Vector3.SignedAngle(breakForecast, currentLine, Vector3.up);

        if (!dontBreak)
        {
            if (Mathf.Abs(forecastAngle) > 20f && car.carSpeed > 60f) car.accelInput = -1f;
            if (Mathf.Abs(forecastAngle) > 30f && car.carSpeed > 50f) car.accelInput = -1f;
            if (Mathf.Abs(forecastAngle) > 40f && car.carSpeed > 40f) car.accelInput = -1f;
            //if (Mathf.Abs(forecastAngle) > 50f && car.carSpeed > 30f) car.accelInput = -1f;
        }
        
        if (car.carSpeed > speedLimit) car.accelInput = -1f;
    }
    
    private void HandleSteering()
    {
        Vector3 origin = transform.position + transform.forward + Vector3.up * 0.5f;

        // Track navigation
        
        float maxDistance = 10f;
        float minDistance = 1f;
        float angle = 30f;
        
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 flatVelocity = car.rbVelocity;
        flatVelocity.y = 0f;
        flatVelocity.Normalize();
        Vector3 averageVector = (flatForward + flatVelocity).normalized;
        
        Ray rayRight = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, angle, 0) * averageVector
        };
        
        Ray rayLeft = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, -angle, 0) * averageVector
        };
        
        Vector3 flatDirection = _currentNodeMarker - transform.position;
        flatDirection.y = 0f;
        
        float signedAngle = Vector3.SignedAngle(flatForward, flatDirection, Vector3.up) / 60f;
        signedAngle = Mathf.Clamp(signedAngle, -1f, 1f);

        float steering = 0f;
        steering += ProcessSteeringRay(rayRight, maxDistance, minDistance, maxDistance, -0.2f, _boundariesLayer);
        steering += ProcessSteeringRay(rayLeft, maxDistance, minDistance , maxDistance, 0.2f, _boundariesLayer);
        steering += signedAngle * 0.3f;

        if (car.carSpeed < 0f) steering = -steering;

        bool retract = (car.steering > 0f && steering < car.steering)
                       || (car.steering < 0f && steering > car.steering);

        float reaction = steeringReaction;

        if (levelManager.player)
        {
            float dist = levelManager.player.GetPlayerDistance() - totalDistance;
            if (dist < 0f) dist = 0f;

            dist *= fallBehindAdjustment;
            
            reaction *= dist + 1f;
        }
        
        // Obstacle detection
        
        maxDistance = 7f;
        minDistance = 2f;
        angle = 15f;
        
        Ray rayForwardRight = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, angle, 0) * flatForward
        };
        
        angle = -angle;
        
        Ray rayForwardLeft = new()
        {
            origin = origin,
            direction = Quaternion.Euler(0, angle, 0) * flatForward
        };

        steering += ProcessSteeringRay(rayRight, maxDistance, minDistance, maxDistance, -0.1f, _carLayer);
        steering += ProcessSteeringRay(rayLeft, maxDistance, minDistance , maxDistance, 0.1f, _carLayer);
        steering += ProcessSteeringRay(rayForwardRight, maxDistance, minDistance, maxDistance, -0.1f, _carLayer);
        steering += ProcessSteeringRay(rayForwardLeft, maxDistance, minDistance , maxDistance, 0.1f, _carLayer);

        float t = Mathf.Clamp01((retract ? 0.7f : 0.5f) * reaction);
        
        car.steering = Mathf.Lerp(car.steering, steering, t);
    }

    private float ProcessSteeringRay(Ray ray, float dist, float minDist, float maxDist, float ratio, int layer)
    {
        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, dist, layer);
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * dist, Color.red);
        
        if (hit)
        {
            if (raycastHit.collider == _selfCollider) return 0f;
            return (1f - GetInterpolatedValue(raycastHit.distance, minDist, maxDist)) * ratio;
        }

        return 0f;
    }

    private float _resetTimer = 0f;
    private float _lastReset = -Mathf.Infinity;
    
    private void HandleReset()
    {
        if (!_isActive || !car.engineOn) return;
        if (_stopCar) return;
        
        if (Time.time - _lastReset < 5f) return;

        if (car.botReset)
        {
            _resetTimer += Time.fixedDeltaTime;
        }
        
        if (car.carSpeed < 2f || Mathf.Abs(_directionAngle) > 80f)
        {
            _resetTimer += Time.fixedDeltaTime;
        }
        else
        {
            _resetTimer = 0f;
        }

        if (_resetTimer >= 5f)
        {
            Reset();
        }
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
        car.botReset = false;
        car.stuckTimer = 0f;
        _resetTimer = 0f;
        _lastReset = Time.time;
        _launchTime = Time.time + 1f;

        Quaternion rot = Quaternion.LookRotation(
            racingLine.orderedNodes[ForecastRacingNode(1)].position -
            racingLine.orderedNodes[currentNodeId].position, Vector3.up);
        car.ResetToPosition(racingLine.orderedNodes[currentNodeId].position, rot);
    }

    public void ActivateBot()
    {
        if (_isActive) return;
        
        car.StartCoroutine(car.StartEngine());
        _isActive = true;
        _launchTime = Time.time + 3f;
        
        FindFirstObjectByType<Minimap>().AddBotMarker(transform);
    }
}
