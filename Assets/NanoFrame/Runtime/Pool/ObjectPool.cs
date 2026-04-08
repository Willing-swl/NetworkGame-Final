using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Core; // 引入我们第一步写的单例

namespace NanoFrame.Pool
{
    /// <summary>
    /// 全局对象池管理器
    /// </summary>
    public class ObjectPool : Singleton<ObjectPool>
    {
        // 我们的“车库”。
        // Key（string）是预制体的名字，比如 "InkDecal"
        // Value（Queue）是排队等待使用的备用物品队列
        private Dictionary<string, Queue<GameObject>> _poolDict = new Dictionary<string, Queue<GameObject>>();

        /// <summary>
        /// 从池子里拿东西 (租车)
        /// </summary>
        /// <param name="prefab">你要拿的预制体模板</param>
        public GameObject Spawn(GameObject prefab)
        {
            string poolKey = prefab.name;

            // 如果字典里还没有这个名字的分类，就建一个新分类
            if (!_poolDict.ContainsKey(poolKey))
            {
                _poolDict.Add(poolKey, new Queue<GameObject>());
            }

            GameObject objToSpawn;

            // 如果池子里有存货
            if (_poolDict[poolKey].Count > 0)
            {
                // Dequeue 的意思是：把队伍最前面的拿出来
                objToSpawn = _poolDict[poolKey].Dequeue();
            }
            else
            {
                // 如果池子空了，没办法，只能临时造一个（Instantiate）
                objToSpawn = Instantiate(prefab);
                objToSpawn.name = poolKey; // 去掉 Unity 自动加的 "(Clone)" 后缀
            }

            // 激活游戏物体
            objToSpawn.SetActive(true);

            // 重点！检查这个物体有没有签过我们的“租借协议(IPoolable)”
            IPoolable poolable = objToSpawn.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawn(); // 叫它执行出场准备
            }

            return objToSpawn;
        }

        /// <summary>
        /// 把东西还回池子 (还车)
        /// </summary>
        public void Unspawn(GameObject obj)
        {
            string poolKey = obj.name;

            // 执行退场清理工作
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnUnspawn();
            }

            // 隐藏游戏物体，相当于从屏幕上拿走
            obj.SetActive(false);

            // 如果这个物体是我们车库名册里的，就放回车库队列；否则直接销毁（防止意外）
            if (_poolDict.ContainsKey(poolKey))
            {
                _poolDict[poolKey].Enqueue(obj); // Enqueue 的意思是：放回队伍最后面排队
            }
            else
            {
                Destroy(obj);
            }
        }
    }
}
