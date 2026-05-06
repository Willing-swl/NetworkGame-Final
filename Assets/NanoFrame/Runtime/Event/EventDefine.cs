using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Rendering;

namespace NanoFrame.Event
{

    public struct OnPlayerDieEvent : IEventMessage
    {
        public int PlayerID; 
    }

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
        public GameObject InkObject;
    }

    public struct OnPlayerStateChangedEvent : IEventMessage
    {
        public int PlayerID;
        public string StateName;
    }

    public struct OnPlayerSprayEvent : IEventMessage
    {
        public int PlayerID;
        public Vector3 Origin;
        public Vector3 Direction;
    }

    public struct OnTileAbsorbedEvent : IEventMessage
    {
        public int PlayerID;
        public int TileID;
        public Vector2Int GridPosition;
    }

    public struct OnPlayerChargeStartedEvent : IEventMessage
    {
        public int PlayerID;
        public int AbsorbedTileCount;
        public int SearchRadius;
    }

    public struct OnPlayerShockwaveEvent : IEventMessage
    {
        public int PlayerID;
        public Vector3 Origin;
        public int AbsorbedTileCount;
        public int ShockwaveTier;
        public float Radius;
        public float Force;
        public float DamagePercent;
    }

    public struct OnPlayerHitEvent : IEventMessage
    {
        public int AttackerPlayerID;
        public int DefenderPlayerID;
        public float DamagePercent;
        public Vector3 Force;
    }

    public struct OnTileCaptureProgressChangedEvent : IEventMessage
    {
        public int TileID;
        public int OwnerPlayerID;
        public float Progress;
        public float Threshold;
    }

    public struct OnMatchTimerChangedEvent : IEventMessage
    {
        public float RemainingTime;
        public float TotalTime;
    }

    public struct OnMatchFinishedEvent : IEventMessage
    {
        public int WinnerPlayerID;
        public bool IsDraw;
        public int Player1TerritoryCount;
        public int Player2TerritoryCount;
    }

    public struct OnDebugBalanceChangedEvent : IEventMessage
    {
        public string FieldName;
        public float Value;
    }

    // 渲染气氛阶段切换事件
    public struct OnRenderMoodPhaseChangedEvent : IEventMessage
    {
        public RenderMoodPhase Phase;
        public bool IsManual;
    }

    public struct TerritoryCountChangedEvent : IEventMessage
    {
        public int PlayerId;        // 1代表P1，2代表P2
        public int MyTileCount;     
        public int TotalTileCount;  // 121
    }
}
