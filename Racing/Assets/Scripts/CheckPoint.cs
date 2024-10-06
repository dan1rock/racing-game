using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private float wideness = 20f;
    [SerializeField] private Transform bar1;
    [SerializeField] private Transform bar2;
    [SerializeField] private CheckPoint next;

    [SerializeField] private LayerMask layerMask;

    public bool isStart = false;
    public bool isActive = false;

    private float _maxHeight;

    private float _lastDistanceRecorded;

    private LevelManager _levelManager;

    private void Awake()
    {
        _levelManager = FindObjectOfType<LevelManager>();
        
        Vector3 newScale = bar1.localScale;
        _maxHeight = newScale.y;

        newScale.y = 0f;

        bar1.localScale = newScale;
        bar2.localScale = newScale;
        
        Activate(isActive);

        if (isStart)
        {
            _levelManager.lastCheckPoint = transform;
            next.Activate(true);
        }
    }

    private void Update()
    {
        Vector3 newScale = bar1.localScale;

        newScale.y = Mathf.Lerp(newScale.y, isActive ? _maxHeight : 0f, 4f * Time.deltaTime);
        
        bar1.localScale = newScale;
        bar2.localScale = newScale;
        
        CheckPlayerDirection();
    }

    private void CheckPlayerDirection()
    {
        if (!isActive) return;
        
        Car player = _levelManager.GetActiveCar();

        Vector3 direction = transform.position - player.transform.position;
        direction.y = 0f;

        Vector3 playerVelocity = player.rbVelocity;
        playerVelocity.y = 0f;

        if (playerVelocity.magnitude < 2f) return;
        
        if (Vector3.Dot(direction, playerVelocity) < 0f && !_levelManager.wrongDirection)
        {
            _lastDistanceRecorded = direction.magnitude;
            _levelManager.WrongDirection(true);
        }

        if (Vector3.Dot(direction, playerVelocity) > 0f && _levelManager.wrongDirection &&
            _lastDistanceRecorded > direction.magnitude)
        {
            _levelManager.WrongDirection(false);
        }
    }

    public void Activate(bool state)
    {
        isActive = state;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        if (other.transform.parent.GetComponent<Car>().playerControlled)
        {
            if (_levelManager.wrongDirection) _levelManager.WrongDirection(false);
            
            _levelManager.OnCheckpoint(transform);
            Activate(false);
            next.Activate(true);
        }
    }
    
    public void SetWideness()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        Vector3 newSize = boxCollider.size;
        newSize.x = wideness;
        boxCollider.size = newSize;

        UpdateBar(bar1, wideness * 0.5f);
        UpdateBar(bar2, -wideness * 0.5f);
    }

    private void UpdateBar(Transform bar, float posX)
    {
        Vector3 newPos = bar.localPosition;
        newPos.x = posX;

        bar.localPosition = newPos;
        
        Ray ray = new()
        {
            origin = bar.position + 5f * Vector3.up,
            direction = Vector3.down
        };

        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, 20f, layerMask, QueryTriggerInteraction.Ignore);
        
        if (hit)
        {
            bar.position = raycastHit.point + Vector3.down;
        }
    }
}
