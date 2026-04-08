using UnityEngine;

namespace NanoFrame.Core
{
    // 全局管理器统一接口
    public interface IManager
    {
        void OnInit();//调用的初始化方法
        void OnUpdate();//统一的帧更新
        void OnDestroyManager();//游戏结束
    }
}