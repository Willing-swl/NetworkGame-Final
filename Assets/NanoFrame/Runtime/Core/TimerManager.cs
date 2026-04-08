using System;
using System.Collections.Generic;
using UnityEngine;

namespace NanoFrame.Core
{
    public class TimerManager : Singleton<TimerManager>
    {
        private class TimerTask
        {
            public float TimeLeft;
            public Action Callback;
            public bool IsCompleted;
        }

        private List<TimerTask> _tasks = new List<TimerTask>();

        public void Register(float delay, Action callback)
        {
            _tasks.Add(new TimerTask { TimeLeft = delay, Callback = callback, IsCompleted = false });
        }

        private void Update()
        {
            if (_tasks.Count == 0) return;

            for (int i = _tasks.Count - 1; i >= 0; i--)
            {
                var task = _tasks[i];
                task.TimeLeft -= Time.deltaTime;

                if (task.TimeLeft <= 0 && !task.IsCompleted)
                {
                    task.IsCompleted = true;
                    task.Callback?.Invoke();
                    _tasks.RemoveAt(i);
                }
            }
        }
    }
}