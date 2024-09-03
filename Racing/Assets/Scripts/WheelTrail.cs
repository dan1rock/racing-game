using UnityEngine;

public class WheelTrail : MonoBehaviour
{
    [SerializeField] private Transform trail;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private float maxVolume = 0.7f;
    [SerializeField] private LayerMask layerMask;

    public bool emitTrail = false;
    public float wheelSpeed = 0f;

    private TrailRenderer _trailRenderer;
    private ParticleSystem _particleSystem;
    private AudioSource _audioSource;

    private Vector3 _trailLastNormal;
    private Vector3 _trailLastPosition;

    private float _driftVolume = 0f;

    private void Awake()
    {
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _audioSource = GetComponent<AudioSource>();

        _audioSource.pitch = Random.Range(0.8f, 1.2f);
    }

    private void Update()
    {
        _trailRenderer.emitting = emitTrail;
        ParticleSystem.EmissionModule emission = _particleSystem.emission;
        emission.enabled = emitTrail;

        float volume = Mathf.Clamp01(wheelSpeed / 100f) * 0.8f + 0.2f;
        float to = emitTrail ? volume * maxVolume : 0f;
        float rate = emitTrail ? 2f : 5f;
        _driftVolume = Mathf.Lerp(_driftVolume, to, Time.deltaTime * rate);
        
        _audioSource.volume = _driftVolume;
        
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
