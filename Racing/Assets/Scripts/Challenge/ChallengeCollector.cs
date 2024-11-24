using System.Collections.Generic;
using UnityEngine;

public class ChallengeCollector : MonoBehaviour
{
    [SerializeField] public int totalCollectibles;

    public int totalCollected;
    
    public void RegisterCollection(StarCollectable collectable)
    {
        ChallengeManager challengeManager = transform.parent.GetComponent<ChallengeManager>();
        GameManager.Get().challengeData[challengeManager.id] = 
            GameManager.Get().challengeData[challengeManager.id] | (1 << collectable.id);

        totalCollected++;
        
        GameManager.Get().SavePlayer();
    }

    public bool GetCollectableState(StarCollectable collectable)
    {
        ChallengeManager challengeManager = transform.parent.GetComponent<ChallengeManager>();
        return (GameManager.Get().challengeData[challengeManager.id] & (1 << collectable.id)) != 0;
    }

    public int GetNumCollected()
    {
        ChallengeManager challengeManager = transform.parent.GetComponent<ChallengeManager>();
        int challengeData = GameManager.Get().challengeData[challengeManager.id];

        int count = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((challengeData & (1 << i)) != 0)
            {
                count++;
            }
        }

        totalCollected = count;
        return count;
    }
}
