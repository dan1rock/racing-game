using UnityEngine;

public class FinishPlaceChallenge : ChallengeRequirement
{
    [SerializeField] private int targetPlace;
    
    public override bool GetCompletionResult()
    {
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        int playerPlace = levelManager.player.currentPosition;

        return playerPlace <= targetPlace;
    }
}
