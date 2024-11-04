using Michsky.MUIP;
using TMPro;
using UnityEngine;

public class UISettings : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown qualityLevelDropdown;
    [SerializeField] private SliderManager masterVolumeSlider;

    private void Start()
    {
        qualityLevelDropdown.value = QualitySettings.GetQualityLevel();
        masterVolumeSlider.mainSlider.value = Settings.Get().masterVolume * 100f;
    }

    public void SetQualityLevel(int index)
    {
        GameManager.Get().SetGraphicsQuality((QualityLevel)index);
    }
}
