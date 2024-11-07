using UnityEngine;

public class ChallengeDriftScore : ChallengeRequirement
{
    [SerializeField] private int targetScore;
    
    public override bool GetCompletionResult()
    {
        DriftManager driftManager = FindFirstObjectByType<DriftManager>();

        return driftManager.GetScore() >= targetScore;
    }
}
