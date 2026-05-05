# 完整数据流：从键盘输入到动画状态机

更新：2026-05-05

---

## 1. 完整调用链路图

```
[ 键盘按键 WASD / Q / Space / F / Shift ]
          │
          ▼
KeyboardPlayerInputSource.ReadFrame()
  - IsHeld(_bindings.Spray)        → SprayHeld
  - IsPressed(_bindings.Jump)      → JumpPressed     (Space)
  - IsHeld(_bindings.Charge)       → ChargeHeld      (Q)
  - IsPressed(_bindings.Dodge)     → DodgePressed    (LShift)
  - ReadMoveVectorInputSystem()    → Move (Vector2)
          │
          ▼ 每帧写入环形缓冲区
PrototypeInputManager._player1Buffer[ring]
          │
          ▼ GameRoot.Update() → PrototypePlayerManager.OnUpdate()
          │ 条件：PrototypeMatchManager.IsRoundActive == true
          │
          ▼
PlayerController.Tick(PlayerInputFrame input, float deltaTime)
  - _currentInput = input               ← CurrentMoveInput 来自这里
  - 更新冷却计时器
  - _stateMachine.Update()
          │
          ▼ FSM 状态机的 OnUpdate() 每帧执行
┌─────────────────────────────────────────────────────┐
│ IdleState / MoveState                               │
│   if JumpPressed      → ChangeState<JumpState>()   │
│   if HasChargeInput   → ChangeState<ChargeState>() │
│   if DodgePressed     → ChangeState<DodgeState>()  │
│   if SprayHeld        → ChangeState<SprayState>()  │
│   MoveState: ApplyMovement() → rigidbody.MovePosition │
└─────────────────────────────────────────────────────┘
          │ 状态切换时调用
          ▼
PlayerController.TriggerStateChanged(string stateName)
  - _currentStateName = stateName   ← PlayerAnimatorBridge 轮询的字段！
  - EventManager.Fire(OnPlayerStateChangedEvent)
          │
          ▼ 每帧 LateUpdate()
PlayerAnimatorBridge.LateUpdate()
  ① 轮询移动输入 → animator.SetFloat("InputX", "InputY")
    (控制 Locomotion 混合树，驱动走/跑/待机动画)
  ② 轮询 controller.CurrentStateName
    - 若与上帧不同 → OnStateChanged()
      - "JumpState"   → animator.SetTrigger("Jump")
      - "ChargeState" → animator.SetTrigger("PowerUp")
      - "DodgeState"  → animator.SetTrigger("Roll")
      - "SprayState"  → animator.SetTrigger("Shoot")
      - "HurtState"   → animator.SetTrigger("Hit")
      ...
          │
          ▼
Animator Controller（Unity 内部）
  - Locomotion Blend Tree：由 InputX / InputY 驱动
  - Any State 连线：Jump / PowerUp / Roll 等 Trigger 触发跳转
  - 动画播放
```

---

## 2. 运行时脚本挂载顺序（都是动态的！）

Prefab `Chicken_P1.prefab` 本身**只是模型**，不包含任何游戏逻辑脚本。
以下挂载全部在游戏启动时由 `PrototypePlayerManager.CreatePlayer()` 完成：

```
游戏启动（第0帧）
  GameRoot.Bootstrap()   ← [RuntimeInitializeOnLoadMethod] 自动触发
  GameRoot.Start()
    ├─ PrototypeInputManager.OnInit()
    ├─ PrototypeMatchManager.OnInit()
    ├─ PrototypeGridManager.OnInit()
    ├─ PrototypePlayerManager.OnInit()
    │     └─ CreatePlayer(1) & CreatePlayer(2)
    │           ① Instantiate(Chicken_P1 prefab)  ← 角色模型出现
    │           ② 删除 PlayerRoamingController（若存在）
    │           ③ AddComponent<PlayerController>   ← 挂到根节点
    │           ④ controller.Initialize(1, ...)    ← FSM 启动
    │           ⑤ AddComponent<PlayerAnimatorBridge> ← 挂到根节点
    └─ RestartMatch()   ← IsRoundActive = true，FSM 开始逐帧 Tick
```

---

## 3. 当前断路位置（已修复）

| 步骤 | 问题 | 修复方式 |
|---|---|---|
| ⑤ AddComponent<PlayerAnimatorBridge> | 原有 [RequireComponent(Animator)] 在根节点创建幽灵 Animator | 删除该 Attribute |
| Bridge.Awake() | Awake 比 Initialize() 早执行，找不到 Controller | 改为 Start() + LateUpdate 懒初始化 |
| Bridge 找 Animator | GetComponent 只搜根节点，真实 Animator 在子节点 | 改为遍历 GetComponentsInChildren，选有 Controller 的那个 |
| 动画触发 | 依赖 EventManager 订阅，存在时序丢失问题 | 改为直接轮询 CurrentStateName |

---

## 4. 验证步骤（请按顺序操作）

### Step 1：确认框架在运行
运行后打开 **Hierarchy** 窗口，应能看到：
- `GameRoot` GameObject
- `PrototypePlayerManager` GameObject
- `PrototypePlayers` GameObject（其下有 `Player_1` 和 `Player_2`）

如果看不到这三个，说明 GameRoot 没有启动，需检查是否有编译错误。

### Step 2：确认 Bridge 绑定成功
Console 里应该能看到：
```
[PlayerAnimatorBridge] P1 成功绑定 Animator：[子节点名称]
[PlayerAnimatorBridge] P2 成功绑定 Animator：[子节点名称]
```
如果看不到 → 说明 Start() 没有执行 → 脚本没被添加

### Step 3：在 Hierarchy 选中 Player_1
- Inspector 里应该能看到 `PlayerController` 和 `PlayerAnimatorBridge` 组件
- 按 WASD 移动，`PlayerController` 里的 `_currentStateName` 字段应该在变

### Step 4：在 Hierarchy 选中动画所在的子节点
- 打开 **Animator 窗口**，此时观察的是子节点（有 AnimatorController 的那个）
- 按 WASD，`InputX`/`InputY` 参数应该在 Animator 参数列表里变化
- 按 Space，`Jump` Trigger 应该闪烁一下

---

## 5. 如果 Step 1 就失败

说明编译有错误，检查：
1. Unity Console 是否有红色 Error 阻止编译
2. PlayerAnimatorBridge.cs 是否保存成功（Ctrl+S）
