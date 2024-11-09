using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChallengeManager : MonoBehaviour
{
    [SerializeField] public int id;
    
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

    [HideInInspector] public List<ChallengeRequirement> challenges = new();
    
    private void Awake()
    {
        challenges.Add(GetActiveChallengeRequirement(transform.GetChild(0).gameObject));
        challenges.Add(GetActiveChallengeRequirement(transform.GetChild(1).gameObject));
        challenges.Add(GetActiveChallengeRequirement(transform.GetChild(2).gameObject));
    }

    [ContextMenu("Start Challenge")]
    public void StartChallenge()
    {
        transform.parent = null;
        GameManager.Get().LoadChallenge(this);
    }

    private static ChallengeRequirement GetActiveChallengeRequirement(GameObject obj)
    {
        ChallengeRequirement[] requirements = obj.GetComponents<ChallengeRequirement>();
        return requirements.FirstOrDefault(req => req.enabled);
    }
}
