using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            // 새로운 GameObject를 만들고 거기에 Dispatcher를 추가함
            _instance = new GameObject("UnityMainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
        }
        return _instance;
    }

    void Update()
    {
        // 매 프레임마다 큐에 있는 작업들을 실행함
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        // 스레드 안전성을 위해 lock을 걸고 큐에 작업을 추가
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
