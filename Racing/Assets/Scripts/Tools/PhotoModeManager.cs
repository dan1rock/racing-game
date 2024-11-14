using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PhotoModeManager : MonoBehaviour
{
    public CinemachineCamera photoModeCamera;
    private bool isPhotoModeActive = false;
    
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float rollSpeed = 2f;

    private Canvas[] _allCanvas;

    private LevelManager _levelManager;
    private CinemachineBrain _cinemachineBrain;
    private CinemachineBlendDefinition _defaultBlend;

    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
        _cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();
        _defaultBlend = _cinemachineBrain.DefaultBlend;

        _allCanvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePhotoMode();
        }

        if (isPhotoModeActive)
        {
            HandleCameraMovement();
            HandleMouseLook();
        }
    }

    public void TogglePhotoMode()
    {
        if (!isPhotoModeActive)
        {
            EnterPhotoMode();
        }
        else
        {
            StartCoroutine(ExitPhotoMode());
        }
    }

    private CursorLockMode _previousCursorState;
    private bool _previousCursorVisible;
    private void EnterPhotoMode()
    {
        _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition
        {
            Style = CinemachineBlendDefinition.Styles.Cut,
            Time = 0f
        };

        transform.position = Camera.main.transform.position;
        transform.rotation = Camera.main.transform.rotation;
        
        Time.timeScale = 0f;
        photoModeCamera.Priority = 100;
        isPhotoModeActive = true;

        _previousCursorState = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Photo mode activated.");
        
        foreach (Canvas canvas in _allCanvas)
        {
            canvas.enabled = false;
        }

        if (_levelManager)
        {
            _levelManager.lastCheckPoint.GetComponent<CheckPoint>().GetNext().gameObject.SetActive(false);
        }
    }

    private IEnumerator ExitPhotoMode()
    {
        Time.timeScale = 1f;
        photoModeCamera.Priority = 0;
        isPhotoModeActive = false;
        Cursor.lockState = _previousCursorState;
        Cursor.visible = _previousCursorVisible;
        Debug.Log("Photo mode deactivated.");
        
        foreach (Canvas canvas in _allCanvas)
        {
            canvas.enabled = true;
        }
        
        if (_levelManager)
        {
            _levelManager.lastCheckPoint.GetComponent<CheckPoint>().GetNext().gameObject.SetActive(true);
        }

        yield return null;
        _cinemachineBrain.DefaultBlend = _defaultBlend;
    }

    private float sprintModifier = 2f;
    private void HandleCameraMovement()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) direction += transform.forward;
        if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
        if (Input.GetKey(KeyCode.A)) direction -= transform.right;
        if (Input.GetKey(KeyCode.D)) direction += transform.right;
        if (Input.GetKey(KeyCode.LeftAlt)) direction -= transform.up;
        if (Input.GetKey(KeyCode.Space)) direction += transform.up;

        float sprint = 1f;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            sprint *= sprintModifier;

            sprintModifier *= 1f + Time.unscaledDeltaTime;
        }
        else
        {
            sprintModifier = 2f;
        }
        
        if (Input.GetKey(KeyCode.LeftControl)) sprint *= 0.5f;
        
        transform.position += direction.normalized * (moveSpeed * Time.unscaledDeltaTime * sprint);
        
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(Vector3.forward, rollSpeed * Time.unscaledDeltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(Vector3.forward, -rollSpeed * Time.unscaledDeltaTime);
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            Vector3 currentEulerAngles = transform.eulerAngles;
            currentEulerAngles.z = 0f;
            transform.eulerAngles = currentEulerAngles;
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        transform.Rotate(Vector3.up, mouseX, Space.World);
        
        transform.Rotate(Vector3.left, mouseY);
    }
}