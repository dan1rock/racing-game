using System;
using UnityEngine;

public class ControlButton : MonoBehaviour
{
    [SerializeField] private ControlKey key;

    public void OnButtonPress()
    {
        Controls.Get().OnButtonDown(key, true);
    }

    public void OnButtonRelease()
    {
        Controls.Get().OnButtonDown(key, false);
    }
}
