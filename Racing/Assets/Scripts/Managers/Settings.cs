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
    [SerializeField] public GraphicsSmoke smokeQuality = GraphicsSmoke.Player;
    [SerializeField] public GraphicsSmoke headlightsQuality = GraphicsSmoke.All;
    
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
            graphicsPreset = GameManager.Get().graphicsQuality;
            smokeQuality = GameManager.Get().smokeQuality;
            headlightsQuality = GameManager.Get().headlightsQuality;
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
        GameManager.Get()?.SaveGraphicsSettings(this);
    }

    public void SetMasterVolume(float value)
    {
        value *= 0.01f;
        
        masterVolume = value;
        AudioListener.volume = masterVolume;

        GameManager.Get().masterVolume = masterVolume;
    }

    private float _preMuteVolume;
    public void MuteGame()
    {
        _preMuteVolume = AudioListener.volume;
        AudioListener.volume = 0f;
    }

    public void RestoreVolume()
    {
        AudioListener.volume = _preMuteVolume;
    }
}
