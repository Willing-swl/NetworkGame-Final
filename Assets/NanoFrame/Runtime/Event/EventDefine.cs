using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NanoFrame.Event
{
    // 以后你的游戏里需要加新事件，全部按照这个格式写在这里！

    // 示例1：玩家死亡事件
    public struct OnPlayerDieEvent : IEventMessage
    {
        public int PlayerID; // 告诉系统是 1P 还是 2P 死了
    }

    // 示例2：重力改变事件
    public struct OnGravityChangedEvent : IEventMessage
    {
        public float NewGravityMultiplier;
    }

    // 示例3：墨水(地块)占领实体化事件
    public struct OnTileCapturedEvent : IEventMessage
    {
        public int PlayerID;
        public int TileID;
    }
    public struct OnInkSolidifiedEvent : IEventMessage
    {
        // 你可以把触发实体化的游戏物体传出去，方便别人获取它的位置或组件
        public GameObject InkObject;
    }
}
