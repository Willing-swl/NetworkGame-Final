# AI 接力说明

如果你是下一位接手这个项目的 AI，先按这个顺序读：
1. [README.md](README.md)
2. [architecture.md](architecture.md)
3. [progress.md](progress.md)
4. [decisions.md](decisions.md)

## 当前共识
- 以 [Inflatable_Paradox_GDD copy.md](../Inflatable_Paradox_GDD%20copy.md) 作为当前基线。
- 后续童核变化不要直接覆盖基线，先写 delta。
- 当前架构是 Event Bus + Manager + FSM，不要把它改成双向绑定式结构。

## 开工前检查
- 先确认你要改的是“基线规范”还是“变体 delta”。
- 先找最近的 progress 和 decisions，再动代码。
- 只要改了运行时行为，就同步更新 architecture 或 progress。

## 结束时必须留下的内容
- 已完成什么。
- 验证了什么。
- 还没有验证什么。
- 当前阻塞点是什么。
- 下一步最小任务是什么。
- 涉及哪些文件。

## 简短模板
当前焦点：

已完成：

已验证：

风险：

下一步：
