using System;
using UnityEngine;

public class ChallengeManager : MonoBehaviour
{
    public DayTime dayTime;
    public Weather weather;
    public int stageId;
    public int laps;
    public int bots = 4;
    public bool stageReverse;
    public GameObject car;
    public Material carColor;
    public RaceMode raceMode;
    public Difficulty difficulty;

    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    [HideInInspector] public ChallengeRequirement challenge1;
    [HideInInspector] public ChallengeRequirement challenge2;
    [HideInInspector] public ChallengeRequirement challenge3;
    
    private void Awake()
    {
        challenge1 = star1.GetComponent<ChallengeRequirement>();
        challenge2 = star2.GetComponent<ChallengeRequirement>();
        challenge3 = star3.GetComponent<ChallengeRequirement>();
    }

    [ContextMenu("Start Challenge")]
    public void StartChallenge()
    {
        GameManager.Get().LoadChallenge(this);
    }
}
