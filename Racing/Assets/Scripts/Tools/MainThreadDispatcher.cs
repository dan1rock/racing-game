using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> ActionQueue = new();
    private static MainThreadDispatcher _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (ActionQueue)
        {
            ActionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (ActionQueue)
        {
            while (ActionQueue.Count > 0)
            {
                ActionQueue.Dequeue()?.Invoke();
            }
        }
    }
}