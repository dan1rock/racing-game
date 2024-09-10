using System;
using System.Collections.Generic;
using UnityEngine;

public enum ControlKey
{
    Accelerate,
    Break,
    Right,
    Left,
    Handbrake,
    ResetCar,
    CycleCar,
    StopEngine
}

public class Controls : MonoBehaviour
{
    [SerializeField] public List<KeyCode> keys;

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
        for (int i = 0; i < _keysNum; i++)
        {
            _keysDown[i] = false;
        }
        
        for (int i = 0; i < keys.Count; i++)
        {
            ProcessKey(keys[i], i);
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
