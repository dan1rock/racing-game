#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class StarCollectable : MonoBehaviour
{
    [SerializeField] private Transform starGraphic;
    [SerializeField] private GameObject starParticles;
    
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatRange = 1f;

    public bool isCollected = false;
    public int id;

    private ChallengeCollector _challengeCollector;

    private void Awake()
    {
        rotationSpeed += Random.Range(-rotationSpeed * 0.1f, rotationSpeed * 0.1f);
        floatSpeed += Random.Range(-floatSpeed * 0.1f, floatSpeed * 0.1f);
        floatRange += Random.Range(-floatRange * 0.1f, floatRange * 0.1f);

        _challengeCollector = FindFirstObjectByType<ChallengeCollector>();
        if (_challengeCollector.GetCollectableState(this))
        {
            isCollected = true;
            starGraphic.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isCollected) return;
        
        starGraphic.position = transform.position + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatRange);
        starGraphic.rotation = Quaternion.Euler(0f, Time.time * rotationSpeed, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.gameObject.layer == 6)
        {
            isCollected = true;
            starGraphic.gameObject.SetActive(false);

            GameObject o = Instantiate(starParticles, starGraphic.position, Quaternion.identity);
            Destroy(o, 5f);
            
            _challengeCollector.RegisterCollection(this);
        }
    }

    [ContextMenu("Update Collectibles ID")]
    public void UpdateCollectiblesId()
    {
        HashSet<int> ids = new();

        StarCollectable[] collectables = FindObjectsByType<StarCollectable>(FindObjectsSortMode.None);
        collectables = collectables
            .OrderBy(c =>
            {
                string name = c.name;
                int startIndex = name.IndexOf('(') + 1;
                int endIndex = name.IndexOf(')');
                
                if (startIndex > 0 && endIndex > startIndex)
                {
                    string numberStr = name.Substring(startIndex, endIndex - startIndex);
                    if (int.TryParse(numberStr, out int number))
                        return number;
                }
                return 0;
            })
            .ToArray();
        
        foreach (StarCollectable collectable in collectables)
        {
            if (ids.Contains(collectable.id))
            {
                for (int i = 0; i < 32; i++)
                {
                    if (ids.Contains(i)) continue;
                    
                    collectable.id = i;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(collectable);    
#endif
                    break;
                }
            }
            
            ids.Add(collectable.id);
        }
    }
}
