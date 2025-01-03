using Michsky.MUIP;
using TMPro;
using UnityEngine;

public class UISettings : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown qualityLevelDropdown;
    [SerializeField] private SliderManager masterVolumeSlider;

    [SerializeField] private TMP_Text graphicsPresetText;
    [SerializeField] private TMP_Text graphicsSmokeText;
    [SerializeField] private TMP_Text graphicsHeadlightsText;
    
    public GraphicsPreset graphicsPreset;
    public GraphicsSmoke graphicsSmoke;
    public GraphicsSmoke graphicsHeadlights;

    private Settings _settings;

    private void Start()
    {
        _settings = FindFirstObjectByType<Settings>();
        
        graphicsPreset = (GraphicsPreset)QualitySettings.GetQualityLevel();
        graphicsSmoke = GameManager.Get().smokeQuality;
        graphicsHeadlights = GameManager.Get().headlightsQuality;
        
        masterVolumeSlider.mainSlider.value = Settings.Get().masterVolume * 100f;

        graphicsPresetText.text = graphicsPreset.ToString();
        graphicsSmokeText.text = graphicsSmoke.ToString();
        graphicsHeadlightsText.text = graphicsHeadlights.ToString();
    }

    public void ScrollGraphicsPreset(bool right)
    {
        graphicsPreset = MenuManager.ClampEnum(graphicsPreset, right);
        
        graphicsPresetText.text = graphicsPreset.ToString();

        _settings.graphicsPreset = (QualityLevel)graphicsPreset;
        _settings.ApplySettings();
    }
    
    public void ScrollGraphicsSmoke(bool right)
    {
        graphicsSmoke = MenuManager.ClampEnum(graphicsSmoke, right);
        
        graphicsSmokeText.text = graphicsSmoke.ToString();

        _settings.smokeQuality = graphicsSmoke;
        _settings.ApplySettings();
    }
    
    public void ScrollGraphicsHeadlights(bool right)
    {
        graphicsHeadlights = MenuManager.ClampEnum(graphicsHeadlights, right);
        
        graphicsHeadlightsText.text = graphicsHeadlights.ToString();

        _settings.headlightsQuality = graphicsHeadlights;
        _settings.ApplySettings();
    }

    public void SetMasterVolume(float value)
    {
        Settings.Get().SetMasterVolume(value);
    }

    public void OnSettingsClose()
    {
        GameManager.Get().SavePlayer();
    }
}
