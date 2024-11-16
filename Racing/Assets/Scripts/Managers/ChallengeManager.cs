#if UNITY_EDITOR
using UnityEditor;
#endif
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

    public List<GameObject> botCars;

    [HideInInspector] public List<ChallengeRequirement> challenges = new();
    
    private void Awake()
    {
        if (botCars.Count == 0) botCars.Add(car);
        
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
    
    public int GetMaxStars()
    {
        return transform.childCount;
    }

    public void SetVacantId()
    {
        HashSet<int> ids = new();

        List<ChallengeManager> managers = new(FindObjectsByType<ChallengeManager>(FindObjectsSortMode.None));
        managers.Remove(this);
        
        foreach (ChallengeManager challengeManager in managers)
        {
            ids.Add(challengeManager.id);
        }

        int vacantId = -1;
        
        for (int i = 0; i < 1024; i++)
        {
            if (ids.Contains(i)) continue;

            vacantId = i;
            break;
        }

        if (vacantId == -1) return;
        
        id = vacantId;
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}
