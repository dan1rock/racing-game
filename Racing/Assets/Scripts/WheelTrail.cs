using UnityEngine;

public class WheelTrail : MonoBehaviour
{
    [SerializeField] private Transform trail;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private LayerMask layerMask;

    public bool emitTrail = false;

    private TrailRenderer _trailRenderer;

    private void Awake()
    {
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    private void Update()
    {
        _trailRenderer.emitting = emitTrail;
        
        Ray ray = new()
        {
            origin = transform.position + transform.up * 0.1f,
            direction = -transform.up
        };
        
        bool hit = Physics.Raycast(ray, out RaycastHit outRay, 0.5f, layerMask);

        if (hit)
        {
            trail.up = outRay.normal;
            trail.position = outRay.point + outRay.normal * groundOffset;
        }
    }
}
