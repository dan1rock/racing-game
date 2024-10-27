using UnityEngine;

public class CarBot : MonoBehaviour
{
    private LayerMask _boundariesLayer = 1 << 9;

    private Car _car;

    private void Awake()
    {
        _car = GetComponent<Car>();
        _car.StartCoroutine(_car.StartEngine());
    }

    private void FixedUpdate()
    {
        _car.accelInput = 1f;
        
        HandleSteering();
    }

    private void HandleSteering()
    {
        Vector3 origin = transform.position + transform.forward;

        float maxDistance = 20f;
        float minDistance = 1f;
        float angle = 30f;
        
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 rotatedDirection = Quaternion.Euler(0, angle, 0) * flatForward;
        
        Ray rayRight = new()
        {
            origin = origin,
            direction = rotatedDirection
        };
        
        angle = -angle;
        
        flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        rotatedDirection = Quaternion.Euler(0, angle, 0) * flatForward;
        
        Ray rayLeft = new()
        {
            origin = origin,
            direction = rotatedDirection
        };
        
        Debug.DrawLine(rayRight.origin, rayRight.origin + rayRight.direction * maxDistance, Color.red);
        Debug.DrawLine(rayLeft.origin, rayLeft.origin + rayLeft.direction * maxDistance, Color.red);

        bool hit = Physics.Raycast(rayRight, out RaycastHit raycastHit, maxDistance, _boundariesLayer);

        _car.steering = 0f;
        if (hit)
        {
            _car.steering -= 1f - GetInterpolatedValue(raycastHit.distance, minDistance, maxDistance);
        }
        
        hit = Physics.Raycast(rayLeft, out raycastHit, maxDistance, _boundariesLayer);

        if (hit)
        {
            _car.steering += 1f - GetInterpolatedValue(raycastHit.distance, minDistance, maxDistance);
        }
        
        Debug.Log(_car.steering);
    }
    
    private float GetInterpolatedValue(float value, float min, float max)
    {
        if (value <= min)
            return 0f;

        if (value >= max)
            return 1f;
        
        return (value - min) / (max - min);
    }

    [ContextMenu("Reset Car")]
    public void Reset()
    {
        _car.InvokeReset(true);
    }
}
