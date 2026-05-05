# 脚本挂载与运行状态全清单

更新时间：2026-05-05

---

## 结论先行：你不需要手动挂任何 Manager 脚本

整个框架使用 `[RuntimeInitializeOnLoadMethod]` + `Singleton<T>` 自动引导。
只要游戏运行，`GameRoot.Bootstrap()` 会自动在场景里创建所有 Manager 的 GameObject 并挂载脚本。

---

## ✅ 自动运行的脚本（不需要手动挂）

| 脚本 | 如何启动 | 职责 |
|------|---------|------|
| `GameRoot` | `[RuntimeInitializeOnLoadMethod]` 自动创建 | 引导整个框架，顺序初始化所有 Manager |
| `PrototypeDebugPanel` | `[RuntimeInitializeOnLoadMethod]` 自动创建 | 屏幕左上角的调试面板 |
| `EventManager` | `GameRoot.Start()` 里 `Singleton.Instance` 自动创建 | 全局事件总线 |
| `PrototypeInputManager` | 同上 | 每帧采样 P1/P2 键盘输入 |
| `PrototypeMatchManager` | 同上 | 回合管理，**初始化后立即调用 `RestartMatch()`** |
| `PrototypeGridManager` | 同上 | 生成地块、涂色、统计领地 |
| `PrototypePlayerManager` | 同上 | **实例化角色 Prefab，添加 PlayerController 和 PlayerAnimatorBridge** |
| `PrototypeSprayVfxManager` | 同上 | 喷射特效 |

> `GameRoot.Start()` 最后一行就是 `PrototypeMatchManager.Instance.RestartMatch()`，
> 所以 **`_roundActive` 在游戏启动后会立即变为 `true`**。

---

## ⚠️ 动画不动的真正原因（已锁定）

`PlayerAnimatorBridge` 在 `Awake()` 里这样拿 Animator：
```csharp
animator = GetComponent<Animator>(); // 只搜索自身 GameObject！
```

但 `PrototypePlayerManager.CreatePlayer()` 是这样挂载它的：
```csharp
playerObject.AddComponent<PlayerAnimatorBridge>(); // 挂到 Prefab 的根节点
```

**你的角色 Prefab（Chicken）的 Animator 几乎肯定在子节点（角色骨骼 Mesh 所在的子物体）上，不在根节点上。**
`GetComponent<Animator>()` 只搜当前节点，所以拿到的是 `null`，动画完全断路！

---

## ✅ 修复方案（改一行）

将 `PlayerAnimatorBridge.cs` 第 28 行：
```csharp
// 改前：只搜索自身
animator = GetComponent<Animator>();

// 改后：搜索自身和所有子节点
animator = GetComponentInChildren<Animator>();
```

---

## 📋 场景里需要手动放置的物体（非脚本，是场景配置）

| 需要配置的内容 | 位置 | 说明 |
|---|---|---|
| `PrototypePlayerManager` Inspector 槽位 | Singleton 自动创建的 GameObject | 把 P1/P2 Prefab 拖到 `_playerPrefabP1` 和 `_playerPrefabP2` 槽 |
| `PrototypeMatchManager` Inspector 槽位 | Singleton 自动创建的 GameObject | 把 `PrototypeBalanceConfig` 资产拖到 `_balanceAsset` 槽 |
| 角色 Prefab 本身 | `Resources/Gameplay/Chicken_P1.prefab` | Manager 会自动 Instantiate，不用放在场景里 |

> 在运行时打开 Hierarchy，找到名为 `PrototypePlayerManager` 的 GameObject，在 Inspector 里就能看到那两个槽位并拖入 Prefab。

---

## 🗑️ 需要从 Prefab 上删掉的脚本

| 脚本 | 状态 | 原因 |
|------|------|------|
| `PlayerRoamingController` (即 `PlayerAnimatorNewInput.cs`) | **可以从 Prefab 上删除** | Manager 已经在运行时调用 `Destroy()` 删它，Prefab 上留着只是增加混乱 |

---

## 📌 动画系统连接关系（正确路径）

```
键盘按键
  ↓
PrototypeInputManager（采样 → PlayerInputFrame）
  ↓
PlayerController.Tick()（FSM 状态机）
  ↓ 状态切换时
EventManager.Fire(OnPlayerStateChangedEvent)
  ↓
PlayerAnimatorBridge.HandlePlayerStateChanged()
  ↓ 触发对应 Trigger
Animator Controller（Jump / PowerUp / Roll / Shoot / Hit…）
  ↓
动画播放
```

**断点就在第5步**：`animator` 是 `null`，所以触发不到任何 Trigger。
改成 `GetComponentInChildren<Animator>()` 后整条链路即可打通。

---

## 总结：你现在只需要做这一件事

> 打开 `PlayerAnimatorBridge.cs` 第 28 行，
> 把 `GetComponent<Animator>()` 改成 `GetComponentInChildren<Animator>()`，
> 保存，运行，动画就通了。
