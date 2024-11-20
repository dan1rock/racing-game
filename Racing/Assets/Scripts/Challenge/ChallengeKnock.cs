using System;
using UnityEngine;

public class ChallengeKnock : ChallengeRequirement
{
    [SerializeField] private int targetKnocked;

    private KnockManager _knockManager;

    public override bool GetCompletionResult()
    {
        if (!_knockManager) _knockManager = FindFirstObjectByType<KnockManager>();
        return _knockManager.knockedObjects >= targetKnocked;
    }
}
