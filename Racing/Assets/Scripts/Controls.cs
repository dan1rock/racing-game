using System;
using UnityEngine;

public enum ControlKey
{
    Accelerate,
    Break,
    Right,
    Left,
    Handbrake,
    ResetCar,
    CycleCar
}

public class Controls : MonoBehaviour
{
    [SerializeField] public KeyCode accelerateKey = KeyCode.W;
    [SerializeField] public KeyCode breakKey = KeyCode.S;
    [SerializeField] public KeyCode rightKey = KeyCode.D;
    [SerializeField] public KeyCode leftKey = KeyCode.A;
    [SerializeField] public KeyCode handBreakKey = KeyCode.Space;
    [SerializeField] public KeyCode resetCarKey = KeyCode.R;
    [SerializeField] public KeyCode cycleCarsKey = KeyCode.T;

    private static Controls _instance;

    private int _keysNum;
    
    private bool[] _keys;
    private bool[] _keysDown;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        _instance = this;

        _keysNum = Enum.GetNames(typeof(ControlKey)).Length;
        _keys = new bool[_keysNum];
        _keysDown = new bool[_keysNum];
    }

    private void Update()
    {
        ProcessKey(accelerateKey, 0);
        ProcessKey(breakKey, 1);
        ProcessKey(rightKey, 2);
        ProcessKey(leftKey, 3);
        ProcessKey(handBreakKey, 4);
        ProcessKey(resetCarKey, 5);
        ProcessKey(cycleCarsKey, 6);
    }

    private void LateUpdate()
    {
        for (int i = 0; i < _keysNum; i++)
        {
            _keysDown[i] = false;
        }
    }

    private void ProcessKey(KeyCode keyCode, int id)
    {
        if (Input.GetKeyDown(keyCode))
        {
            _keys[id] = true;
            _keysDown[id] = true;
        }
        
        if (Input.GetKeyUp(keyCode))
        {
            _keys[id] = false;
        }
    }

    public static Controls Get()
    {
        if (!_instance) _instance = FindObjectOfType<Controls>();
        return _instance;
    }

    public void OnButtonDown(ControlKey key, bool down)
    {
        _keys[(int)key] = down;
        
        if (down)
        {
            _keysDown[(int)key] = true;
        }
    }

    public bool GetKey(ControlKey key)
    {
        return _keys[(int)key];
    }
    
    public bool GetKeyDown(ControlKey key)
    {
        return _keysDown[(int)key];
    }
}
