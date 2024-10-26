#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private float wideness = 20f;
    [SerializeField] private Transform bar1;
    [SerializeField] private Transform bar2;
    [SerializeField] private CheckPoint next;
    private CheckPoint prev;

    [SerializeField] private LayerMask layerMask;

    public bool isStart = false;
    public bool isActive = false;

    private float _maxHeight;

    private float _lastDistanceRecorded;

    private float _wrongDirectionTime = 0f;

    private LevelManager _levelManager;

    private void Awake()
    {
        _levelManager = FindObjectOfType<LevelManager>();

        next.prev = this;
        
        Vector3 newScale = bar1.localScale;
        _maxHeight = newScale.y;

        newScale.y = 0f;

        bar1.localScale = newScale;
        bar2.localScale = newScale;
        
        Activate(isActive);
    }

    private void Start()
    {
        if (isStart)
        {
            _levelManager.lastCheckPoint = this;
            if (_levelManager.reverse)
            {
                prev.Activate(true);
            }
            else
            {
                next.Activate(true);
            }

            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.material.SetColor("_BaseColor", Color.red);
                meshRenderer.material.SetColor("_EmissionColor", Color.red * 2f);
            }
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

        bool wrongDirection = Vector3.Dot(direction, playerVelocity) < 0f;
        _levelManager.wrongDirection = wrongDirection;

        if (wrongDirection)
        {
            _wrongDirectionTime += Time.deltaTime;
        }
        else
        {
            _wrongDirectionTime = 0f;
        }
        
        if (wrongDirection && !_levelManager.wrongDirectionActive && _wrongDirectionTime > 0.5f)
        {
            _lastDistanceRecorded = direction.magnitude;
            _levelManager.WrongDirection(true);
        }

        if (!wrongDirection && _levelManager.wrongDirectionActive && _lastDistanceRecorded > direction.magnitude)
        {
            _levelManager.WrongDirection(false);
        }
    }

    public void Activate(bool state)
    {
        isActive = state;
    }

    public CheckPoint GetNext()
    {
        return _levelManager.reverse ? prev : next;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        if (other.transform.parent.GetComponent<Car>().playerControlled)
        {
            if (_levelManager.wrongDirectionActive) _levelManager.WrongDirection(false);
            
            _levelManager.OnCheckpoint(this);
            if (isStart) _levelManager.LapFinished();
            Activate(false);
            
            GetNext().Activate(true);
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
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(transform);
        EditorUtility.SetDirty(bar1.transform);
        EditorUtility.SetDirty(bar2.transform);
        EditorUtility.SetDirty(boxCollider);
#endif
    }

    public void UpdateAllCheckpoints()
    {
        CheckPoint[] checkPoints = FindObjectsOfType<CheckPoint>();

        foreach (CheckPoint checkPoint in checkPoints)
        {
            checkPoint.SetWideness();
        }
    }

    private void UpdateBar(Transform bar, float posX)
    {
        Vector3 newPos = bar.localPosition;
        newPos.x = posX;

        bar.localPosition = newPos;
        
        Ray ray = new()
        {
            origin = bar.position + 15f * Vector3.up,
            direction = Vector3.down
        };

        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, 30f, layerMask, QueryTriggerInteraction.Ignore);
        
        if (hit)
        {
            bar.position = raycastHit.point + Vector3.down;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
