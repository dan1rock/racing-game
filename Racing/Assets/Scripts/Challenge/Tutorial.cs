using System;
using System.Collections;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject gasGuide;
    [SerializeField] private GameObject breakGuide;
    [SerializeField] private GameObject handBreakGuide;
    [SerializeField] private GameObject rewindGuide;
    [SerializeField] private GameObject rewindStopGuide;

    private Controls _controls;

    private void Awake()
    {
        _controls = Controls.Get();
    }

    private void Start()
    {
        StartCoroutine(TutorialRoutine());
    }

    private IEnumerator TutorialRoutine()
    {
        gasGuide.SetActive(true);

        while (!_controls.GetKey(ControlKey.Accelerate))
        {
            yield return null;
        }
        
        gasGuide.SetActive(false);

        yield return new WaitForSeconds(15f);
        
        Time.timeScale = 0f;
        
        breakGuide.SetActive(true);

        while (!_controls.GetKey(ControlKey.Break))
        {
            yield return null;
        }
        
        breakGuide.SetActive(false);
        
        Time.timeScale = 1f;
        
        yield return new WaitForSeconds(10f);
        
        Time.timeScale = 0f;
        
        handBreakGuide.SetActive(true);

        while (!_controls.GetKey(ControlKey.Handbrake))
        {
            yield return null;
        }
        
        handBreakGuide.SetActive(false);
        
        Time.timeScale = 1f;
        
        yield return new WaitForSeconds(2f);
        
        Time.timeScale = 0f;
        
        rewindGuide.SetActive(true);

        while (!_controls.GetKey(ControlKey.Rewind))
        {
            yield return null;
        }
        
        rewindGuide.SetActive(false);
        
        yield return new WaitForSecondsRealtime(3f);
        
        rewindStopGuide.SetActive(true);

        while (!_controls.GetKey(ControlKey.Accelerate))
        {
            yield return null;
        }
        
        rewindStopGuide.SetActive(false);
    }
}
