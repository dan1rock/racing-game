using System;
using UnityEngine;

public class Controls : MonoBehaviour
{
    [SerializeField] public KeyCode accelerateKey = KeyCode.W;
    [SerializeField] public KeyCode breakKey = KeyCode.S;
    [SerializeField] public KeyCode rightKey = KeyCode.D;
    [SerializeField] public KeyCode leftKey = KeyCode.A;
    [SerializeField] public KeyCode handBreakKey = KeyCode.Space;
    [SerializeField] public KeyCode resetCarKey = KeyCode.R;
    [SerializeField] public KeyCode cycleCarsKey = KeyCode.T;

    private static Controls _controls;

    private void Awake()
    {
        if (_controls)
        {
            DestroyImmediate(gameObject);
            return;
        }

        _controls = this;
    }

    public static Controls Get()
    {
        return _controls;
    }
}
