using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NanoFrame.Pool
{
    /// <summary>
    /// 可回收对象接口
    /// 作用：任何想要放进对象池的东西（比如墨水、子弹、特效），都必须遵守这个“租借协议”
    /// </summary>
    public interface IPoolable
    {
        // 当对象从池子里拿出来时，要做什么？（比如：播放出现音效、重置生命值）
        void OnSpawn();

        // 当对象被放回池子时，要做什么？（比如：停止发光、清除身上的物理力）
        void OnUnspawn();
    }
}
