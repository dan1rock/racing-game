using TMPro;
using UnityEngine;

public class UISettings : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown qualityLevelDropdown;

    private void Start()
    {
        qualityLevelDropdown.value = QualitySettings.GetQualityLevel();
    }

    public void SetQualityLevel(int index)
    {
        GameManager.Get().SetGraphicsQuality((QualityLevel)index);
    }
}
