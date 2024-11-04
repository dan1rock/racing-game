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
    [SerializeField] public float masterVolume = 1f;
    [SerializeField] public QualityLevel graphicsPreset = QualityLevel.Medium;
    
    private static Settings _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        _instance = this;
        
        DontDestroyOnLoad(gameObject);

        if (GameManager.Get())
        {
            GameManager.Get().settings = this;
            graphicsPreset = GameManager.Get().graphicsQuality;
            masterVolume = GameManager.Get().masterVolume;
        }
        
        ApplySettings();
    }

    public static Settings Get()
    {
        return _instance;
    }

    private void SetQuality(QualityLevel qualityLevel)
    {
        QualitySettings.SetQualityLevel((int) qualityLevel, false);
    }

    [ContextMenu("Apply settings")]
    public void ApplySettings()
    {
        AudioListener.volume = masterVolume;
        SetQuality(graphicsPreset);
    }

    public void SetMasterVolume(float value)
    {
        value *= 0.01f;
        
        masterVolume = value;
        AudioListener.volume = masterVolume;

        GameManager.Get().masterVolume = masterVolume;
    }
}
