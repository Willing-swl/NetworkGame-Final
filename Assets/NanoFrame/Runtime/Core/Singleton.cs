using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NanoFrame.Core
{
    /// <summary>
    /// 自动创建的单例基类
    /// 作用：让一个类在全游戏里只有一个实例，且可以随时随地通过 类名.Instance 访问
    /// </summary>
    /// <typeparam name="T">想要变成单例的类名</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 在场景中寻找是否已经存在这个管家
                    _instance = FindObjectOfType<T>();

                    // 如果场景里没有，我们就自动创建一个名为“管家”的游戏物体
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();

                        // 告诉 Unity：切场景的时候不要销毁这个管家
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
    }
}
