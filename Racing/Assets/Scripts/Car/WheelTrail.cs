using UnityEngine;

public class WheelTrail : MonoBehaviour
{
    [SerializeField] private Transform trail;
    [SerializeField] private ParticleSystem tireParticles;
    [SerializeField] private ParticleSystem offRoadParticles;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private float maxVolume = 0.7f;
    [SerializeField] private LayerMask layerMask;
    
    public bool emitTrail = false;
    public float wheelSpeed = 0f;
    public int surfaceLayer;

    public Car car;

    private Settings _settings;
    private TrailRenderer _trailRenderer;
    private AudioSource _audioSource;

    private Vector3 _trailLastNormal;
    private Vector3 _trailLastPosition;

    private float _driftVolume = 0f;

    private void Awake()
    {
        _settings = FindFirstObjectByType<Settings>();
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
        _audioSource = GetComponent<AudioSource>();

        _audioSource.pitch = Random.Range(0.8f, 1.2f);
    }

    private void Update()
    {
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

            Vector3 newRotation = trail.eulerAngles;
            newRotation.y = transform.eulerAngles.y;
            trail.rotation = Quaternion.Euler(newRotation);

            _trailLastNormal = trail.up;
            _trailLastPosition = trail.position;
        }
        else
        {
            trail.up = _trailLastNormal;

            Vector3 pos = transform.position;

            pos.y = _trailLastPosition.y;
            
            trail.position = pos;
        }
        
        _trailRenderer.emitting = emitTrail && hit && surfaceLayer == 7;

        bool smokeEmission = _settings.smokeQuality == GraphicsSmoke.All ||
                             (_settings.smokeQuality == GraphicsSmoke.Player && (!car.isBot || car.menuMode));
        
        ParticleSystem.EmissionModule tireParticlesEmission = tireParticles.emission;
        tireParticlesEmission.enabled = emitTrail && hit && surfaceLayer == 7 && smokeEmission;
        
        ParticleSystem.EmissionModule offRoadParticlesEmission = offRoadParticles.emission;
        offRoadParticlesEmission.enabled = emitTrail && hit && surfaceLayer != 7 && smokeEmission;

        float volume = Mathf.Clamp01(wheelSpeed / 50f) * 0.8f + 0.2f;
        float to = emitTrail && hit && surfaceLayer == 7 ? volume * maxVolume : 0f;
        float rate = emitTrail && hit && surfaceLayer == 7 ? 2f : 5f;
        _driftVolume = Mathf.Lerp(_driftVolume, to, Time.deltaTime * rate);
        
        _audioSource.volume = _driftVolume;
    }
}
