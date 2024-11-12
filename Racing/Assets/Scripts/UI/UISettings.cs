using Michsky.MUIP;
using TMPro;
using UnityEngine;

public enum GraphicsPreset
{
    Low,
    Medium,
    High
}

public class UISettings : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown qualityLevelDropdown;
    [SerializeField] private SliderManager masterVolumeSlider;

    [SerializeField] private TMP_Text graphicsPresetText;
    
    public GraphicsPreset graphicsPreset;

    private void Start()
    {
        graphicsPreset = (GraphicsPreset)QualitySettings.GetQualityLevel();
        masterVolumeSlider.mainSlider.value = Settings.Get().masterVolume * 100f;

        graphicsPresetText.text = graphicsPreset.ToString();
    }

    public void ScrollGraphicsPreset(bool right)
    {
        graphicsPreset = MenuManager.ClampEnum(graphicsPreset, right);
        
        graphicsPresetText.text = graphicsPreset.ToString();

        SetQualityLevel((int)graphicsPreset);
    }
    
    public void SetQualityLevel(int index)
    {
        GameManager.Get().SetGraphicsQuality((QualityLevel)index);
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
