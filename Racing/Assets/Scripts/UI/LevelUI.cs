using UnityEngine;

public class LevelUI : MonoBehaviour
{
    public void LoadMenu()
    {
        Time.timeScale = 1f;
        GameManager.Get()?.SetCarVolume(1f);
        GameManager.Get()?.LoadMenu();
    }

    public void OnPause()
    {
        GameManager.Get()?.SetCarVolume(0f);
        Time.timeScale = 0f;
    }

    public void OnResume()
    {
        GameManager.Get()?.SetCarVolume(1f);
        Time.timeScale = 1f;
    }

    public void OnRestart()
    {
        Time.timeScale = 1f;
        GameManager.Get()?.SetCarVolume(1f);
        GameManager.Get()?.ReloadStage();
    }
}
