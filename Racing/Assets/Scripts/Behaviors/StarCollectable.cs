using System;
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
    public int collectableId;

    private void Awake()
    {
        rotationSpeed += Random.Range(-rotationSpeed * 0.1f, rotationSpeed * 0.1f);
        floatSpeed += Random.Range(-floatSpeed * 0.1f, floatSpeed * 0.1f);
        floatRange += Random.Range(-floatRange * 0.1f, floatRange * 0.1f);
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
        }
    }
}
