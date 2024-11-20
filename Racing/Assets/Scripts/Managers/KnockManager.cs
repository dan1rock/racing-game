using System.Collections.Generic;
using UnityEngine;

public class KnockManager : MonoBehaviour
{
    public int totalObjects = 0;
    public int knockedObjects = 0;

    private List<KnockDownObject> _knockObjects = new();

    public void RegisterObject(KnockDownObject knockDownObject)
    {
        _knockObjects.Add(knockDownObject);
        totalObjects += 1;
    }

    public void ObjectKnocked(KnockDownObject knockDownObject)
    {
        if (_knockObjects.Remove(knockDownObject))
        {
            knockedObjects += 1;
        }
    }
}
