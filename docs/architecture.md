# 当前架构快照

更新时间：2026-04-22

这份文档只描述当前已经能在代码里确认的运行时结构，不重写 GDD 里的设计愿景。它的作用是让下一位接手的人快速知道：谁负责什么、数据怎么流、事件从哪里发、状态怎么切。

## 1. 核心骨架

- [PrototypeMatchManager](../Assets/_Project/Scripts/Gameplay/Match/PrototypeMatchManager.cs) 负责回合生命周期、暂停、重开、计时和终局判定。
- [PrototypeInputManager](../Assets/_Project/Scripts/Gameplay/Input/PrototypeInputManager.cs) 负责采样输入、保存输入缓存帧、提供当前帧和历史帧。
- [PrototypePlayerManager](../Assets/_Project/Scripts/Gameplay/Player/PrototypePlayerManager.cs) 负责生成玩家、重置玩家、每帧推动玩家逻辑。
- [PrototypeGridManager](../Assets/_Project/Scripts/Gameplay/Grid/PrototypeGridManager.cs) 负责生成格子、涂色、占领、吸收、统计领地数量。
- [PlayerController](../Assets/_Project/Scripts/Gameplay/Player/PlayerController.cs) 负责单个玩家的 FSM、移动、喷射、蓄力、闪避、受击、击飞和淘汰。
- [EventManager](../Assets/NanoFrame/Runtime/Event/EventManager.cs) 负责强类型事件总线。
- [PrototypeDebugPanel](../Assets/_Project/Scripts/Gameplay/Debug/PrototypeDebugPanel.cs) 负责调试面板、运行时调参和事件回显。
- [PrototypeBalanceConfig](../Assets/_Project/Scripts/Gameplay/Config/PrototypeBalanceConfig.cs) 负责保存可调数值。

## 2. 数据流

### 输入到行动
1. `PrototypeInputManager` 每帧采样两名玩家输入。
2. `PrototypePlayerManager` 在回合激活时把输入帧传给每个 `PlayerController`。
3. `PlayerController` 在 FSM 内决定当前状态是否进入喷射、蓄力、闪避、移动、受击或击飞。

### 行动到场地
1. 喷射由 `PlayerController` 调用 `PrototypeGridManager.TryApplySpray`。
2. 蓄力由 `PlayerController` 调用 `PrototypeGridManager.AbsorbTilesForShockwave`。
3. 场地占领、吸收和进度变化由 `PrototypeGridManager` 维护计数并广播事件。

### 行动到事件
`PlayerController` 会广播这些事件：
- `OnPlayerStateChangedEvent`
- `OnPlayerSprayEvent`
- `OnPlayerChargeStartedEvent`
- `OnPlayerShockwaveEvent`
- `OnPlayerHitEvent`
- `OnPlayerDieEvent`

`PrototypeGridManager` 会广播这些事件：
- `OnTileCaptureProgressChangedEvent`
- `OnTileCapturedEvent`
- `OnTileAbsorbedEvent`

`PrototypeMatchManager` 会广播这些事件：
- `OnMatchTimerChangedEvent`
- `OnMatchFinishedEvent`

## 3. 状态机

`PlayerController` 内部状态目前固定为：

- `Idle`
- `Move`
- `Spray`
- `Charge`
- `Dodge`
- `Hurt`
- `Knockback`
- `Eliminated`

关键行为约束：

- `Idle` 和 `Move` 都会在输入满足条件时切到 `Charge`、`Dodge` 或 `Spray`。
- `Spray` 在松开喷射后通过 `FinishActionState` 回到 `Charge`、`Dodge`、`Move` 或 `Idle`。
- `Charge` 进入时会吸收己方格子，触发 `OnPlayerChargeStartedEvent`，结束时释放冲击波并触发 `OnPlayerShockwaveEvent`。
- `Hurt` 只是短暂受击过渡，随后切到 `Knockback`。
- `Knockback` 根据受伤值调整位移强度，结束后再回到可操作状态。
- `Eliminated` 会触发 `OnPlayerDieEvent`，由回合管理器负责最终结算。

## 4. 配置与约束

- `PrototypeBalanceConfig` 是当前唯一的运行时数值入口。
- `InputBufferSize` 当前默认是 10 帧缓存。
- `DeadZone` 默认是 0.2。
- `GridWidth` 和 `GridHeight` 当前默认都是 11。
- `RoundDuration` 当前默认是 60 秒。

## 5. 当前可以直接接手的边界

- 想改回合规则，先看 `PrototypeMatchManager`。
- 想改输入、缓存或暂停，先看 `PrototypeInputManager`。
- 想改玩家动作链，先看 `PlayerController`。
- 想改涂色、吸收和领地统计，先看 `PrototypeGridManager`。
- 想改调参 UI 或现场验证，先看 `PrototypeDebugPanel`。
