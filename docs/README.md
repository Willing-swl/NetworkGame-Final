# 项目文档索引

当前 canonical baseline 是 [Inflatable_Paradox_GDD copy.md](../Inflatable_Paradox_GDD%20copy.md)。
[Inflatable_Paradox_GDD.md](../Inflatable_Paradox_GDD.md) 仅作为历史对照，不作为当前开发基线。

## 阅读顺序
1. [architecture.md](architecture.md)
2. [progress.md](progress.md)
3. [decisions.md](decisions.md)
4. [handoff.md](handoff.md)

## 规则
- 基线文档只做稳定规范，不直接覆盖后续风格变体。
- 后续从梦核切到童核的内容，统一记为 delta，不回写基线。
- 新的实现事实先记进 decisions 或 progress，再考虑是否需要写回架构文档。
- 下一位 AI 接手时，先读 docs，再读代码，不要先翻聊天记录。

## 现在有哪些文档
| 文档 | 用途 |
|---|---|
| [architecture.md](architecture.md) | 当前运行时架构、事件流、状态流、模块边界 |
| [decisions.md](decisions.md) | 关键技术选择、弃用项、历史取舍 |
| [progress.md](progress.md) | 当前进度、已验证事项、下一步 |
| [handoff.md](handoff.md) | 给下一位 AI 或协作者的接力说明 |
| [variants/README.md](variants/README.md) | 基线之上的主题或版本分支说明 |
