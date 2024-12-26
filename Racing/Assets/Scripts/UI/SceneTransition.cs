using System.Collections;
using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private GameObject transitionIn;
    [SerializeField] private GameObject transitionOut;

    public void PlayTransitionIn()
    {
        transitionIn.SetActive(false);
        transitionIn.SetActive(true);

        StartCoroutine(VolumeRoutine(Settings.Get().masterVolume));
    }
    
    public void PlayTransitionOut()
    {
        transitionOut.SetActive(false);
        transitionOut.SetActive(true);
        
        StartCoroutine(VolumeRoutine(0f));
    }

    private IEnumerator VolumeRoutine(float to)
    {
        float from = AudioListener.volume;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.unscaledDeltaTime;
            if (progress > 1f) progress = 1f;

            AudioListener.volume = Mathf.Lerp(from, to, progress);

            yield return null;
        }
    }
}
