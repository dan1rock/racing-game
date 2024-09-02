using UnityEngine;

public class WheelTrail : MonoBehaviour
{
    [SerializeField] private Transform trail;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private LayerMask layerMask;

    public bool emitTrail = false;

    private TrailRenderer _trailRenderer;
    private ParticleSystem _particleSystem;

    private Vector3 _trailLastNormal;
    private Vector3 _trailLastPosition;

    private void Awake()
    {
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        _trailRenderer.emitting = emitTrail;
        ParticleSystem.EmissionModule emission = _particleSystem.emission;
        emission.enabled = emitTrail;
        
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

            _trailLastNormal = trail.up;
            _trailLastPosition = trail.position;
        }
        else
        {
            trail.up = _trailLastNormal;
            trail.position = _trailLastPosition;
        }
    }
}
