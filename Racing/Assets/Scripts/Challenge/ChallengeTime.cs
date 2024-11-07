using UnityEngine;

public class ChallengeTime : ChallengeRequirement
{
    [SerializeField] private float targetTime;
    
    public override bool GetCompletionResult()
    {
        TimeAttackManager timeAttackManager = FindFirstObjectByType<TimeAttackManager>();

        return timeAttackManager.overallTime <= targetTime;
    }
}
