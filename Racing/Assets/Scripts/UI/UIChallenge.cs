using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIChallenge : MonoBehaviour
{
    [SerializeField] private GameObject defaultStars;
    [SerializeField] private GameObject collectableStars;

    [SerializeField] private TMP_Text collectedText;
        
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
        challengeNumber.text = (transform.GetSiblingIndex() + 1).ToString();
        
        if (GetComponentInChildren<ChallengeCollector>())
        {
            DisplayCollectableChallenge();
        }
        else
        {
            DisplayDefaultChallenge();
        }
    }

    private void DisplayCollectableChallenge()
    {
        collectableStars.SetActive(true);
        defaultStars.SetActive(false);
        
        ChallengeCollector challengeCollector = GetComponentInChildren<ChallengeCollector>();

        int collectedStars = challengeCollector.GetNumCollected();
        int totalStars = challengeCollector.totalCollectibles;

        collectedText.text = $"{collectedStars} / {totalStars}";
    }

    private void DisplayDefaultChallenge()
    {
        collectableStars.SetActive(false);
        defaultStars.SetActive(true);
        
        for (int i = 0; i < stars.Length; i++)
        {
            if ((GameManager.Get().challengeData[_challengeManager.id] & (1 << i)) != 0)
            {
                stars[i].sprite = starFilled;
            }
        }
    }
}
