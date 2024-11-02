using UnityEngine;
using UnityEngine.UI;

public class ButtonKeyPress : MonoBehaviour
{
    public KeyCode triggerKey = KeyCode.Space;

    private Button _uiButton;

    private void Awake()
    {
        _uiButton = GetComponent<Button>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            _uiButton.onClick.Invoke();
        }
    }
}