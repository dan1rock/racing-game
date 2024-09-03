using System;
using UnityEngine;

public enum QualityLevel
{
    Low,
    Medium,
    High
}

public class Settings : MonoBehaviour
{
    private static Settings _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        _instance = this;
    }

    public static Settings Get()
    {
        return _instance;
    }

    public void SetQuality(QualityLevel qualityLevel)
    {
        QualitySettings.SetQualityLevel((int) qualityLevel, false);
    }
}
