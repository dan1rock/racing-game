using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIChallenge : MonoBehaviour
{
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Image[] stars;
    [SerializeField] private TMP_Text challengeNumber;

    private ChallengeManager _challengeManager;

    private void Awake()
    {
        _challengeManager = GetComponentInChildren<ChallengeManager>();
    }

    private void Start()
    {
        for (int i = 0; i < stars.Length; i++)
        {
            if ((GameManager.Get().challengeData[_challengeManager.id] & (1 << i)) != 0)
            {
                stars[i].sprite = starFilled;
            }
        }

        challengeNumber.text = (transform.GetSiblingIndex() + 1).ToString();
    }
}
