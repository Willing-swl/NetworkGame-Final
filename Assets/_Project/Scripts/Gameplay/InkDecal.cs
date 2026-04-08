using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Pool;   // 引入框架的对象池模块
using NanoFrame.Event;  // 引入框架的事件模块

namespace Project.Gameplay
{
    /// <summary>
    /// 墨水/水泡弹贴花脚本（业务逻辑）
    /// 结合《充气悖论》GDD，这可能就是你的水泡弹击中地块后延迟生效的逻辑
    /// </summary>
    public class InkDecal : MonoBehaviour, IPoolable
    {
        [Header("延迟实体化/爆炸时间")]
        public float solidifyDelay = 3f;
        private float _timer;
        private bool _isSolidified; // 是否已经实体化

        // 1. 实现接口：当对象池把这个物体拿出来用时，自动执行
        public void OnSpawn()
        {
            // 每次拿出来，必须“重置洗白”所有状态！避免上一发的计时器污染这一发
            _timer = 0f;
            _isSolidified = false;

            // 表现层：设置为半透明（预告态），关闭碰撞器
            Debug.Log("水泡弹被射出！当前是预告态。");
        }

        // 2. 实现接口：当对象池把这个物体回收时，自动执行
        public void OnUnspawn()
        {
            // 回收时的清理工作（比如停止身上的粒子特效）
            Debug.Log("水泡弹被回收进对象池了，清理状态。");
        }

        private void Update()
        {
            // 如果还没实体化，就开始倒计时
            if (!_isSolidified)
            {
                _timer += Time.deltaTime;
                if (_timer >= solidifyDelay)
                {
                    Solidify();
                }
            }
        }

        // 实体化 / 爆炸占领逻辑
        private void Solidify()
        {
            _isSolidified = true;
            Debug.Log("时间到！水泡弹爆炸/实体化！");

            // 表现层：变成高光发光材质，开启碰撞器等操作...

            // 【架构师重点划线】：使用全新的零 GC 强类型事件中心触发！
            // 这样写不仅不产生任何内存垃圾，还能利用 IDE 直接点进去看事件定义
            EventManager.Instance.Fire(new OnInkSolidifiedEvent { InkObject = this.gameObject });
        }

        // 提供给外部或自己调用的回收方法
        public void RecycleInk()
        {
            // 不要用 Destroy(gameObject)！而是把车还给“租车公司”
            ObjectPool.Instance.Unspawn(this.gameObject);
        }
    }
}