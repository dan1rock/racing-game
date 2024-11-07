using UnityEngine;

public abstract class ChallengeRequirement: MonoBehaviour
{
    public string caption;
    
    public abstract bool GetCompletionResult();
}
