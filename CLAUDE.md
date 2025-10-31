# CLAUDE.md

**⚠️ 新会话开始必读**: 这是跨会话记忆的主要入口文件！

## 🎯 项目速览

这是一个为游戏《逃离鸭科夫》开发的 Unity Mod，主要功能是**背包配件系统**，允许为背包添加配件插槽和各种配件物品，支持嵌套插槽功能。

## 📁 核心记忆体系

### 🚀 主要入口（当前文件）
- **作用**: 新会话快速启动指南
- **内容**: 最关键的核心信息，凝练到最少
- **使用**: 每次新会话开始时必须阅读

### 📚 详细记忆库
**位置**: `docs/memory/` 目录
- **分类存储**: 技术细节、成功案例、常见陷阱等
- **扩展内容**: 当前文件的详细版本和补充信息
- **检索方式**: 通过具体需求查找对应分类文件

## 🔥 核心中的核心（必须记住）

### 1. 源码位置铁律 ⚠️
```
GameSource/Duckov/ - 游戏官方源码（第一优先级）
```
**关键文件**:
- `Item.cs` - 物品基类
- `Inventory.cs` - 背包容器系统
- `ItemDisplay.cs` - 物品交互 (487-530行: OnPointerClick)
- `DialogueBubble.cs` - 气泡系统 (137行: defaultSpeed = 10f)
- `CharacterMainControl.cs` - 角色控制 (PopText方法)

### 2. 源码驱动开发原则 🔥 【最高优先级】
**绝对禁止**: 基于经验或参数名称猜测功能
**标准流程**: 查看源码 → 理解参数 → 基于源码实现

### 3. Item类使用规范 ⚠️
```csharp
// ✅ 正确
int count = item.Slots.Count;   // SlotCollection用Count

// ❌ 致命错误
int count = item.Slots.Length;  // 编译错误！
```

### 4. 关键参数实例（气泡速度）⚡
- **位置**: `DialogueBubble.cs:137` - `defaultSpeed = 10f`
- **真相**: speed数值**越大越快**（不是越小越快）
- **正确**: `playerCharacter.PopText(text, 50f)` // 5倍速

### 5. 反射操作安全
```csharp
if (_field == null) return;                    // 必须null检查
GameObject obj = _field.GetValue(instance) as GameObject;
if (obj == null) return;                      // 二次检查
Object.DestroyImmediate(obj);                 // 同步删除
```

## 🎯 快速检索模式

**物品/背包问题** → `GameSource/Duckov/Item*.cs`
**UI交互问题** → `ItemDisplay.cs` + `AttachmentUI/`
**配置修改** → `BackpackModConfig.cs`
**任务查询** → `docs/TODO-待做清单.md`
**详细记忆** → `docs/memory/` 目录

## 📋 会话启动检查清单

新会话开始时必须：
1. ✅ 阅读当前文件（CLAUDE.md）
2. ✅ 知道项目是背包配件Mod
3. ✅ 记住源码在GameSource/Duckov/
4. ✅ 理解源码驱动开发原则
5. ✅ 查看TODO文件了解当前任务

## 🎯 任务完成守则 【最高优先级】

### 三层持久化方法论
**"每完成一个重要任务，必须完成三层持久化记录，一个都不能少！"**

1. **Git提交记录** - 代码变更历史和可追溯性
2. **项目文档更新** - TODO清单、CHANGELOG状态更新
3. **技术设计文档** - 完整的实现细节和经验总结

**详细说明**: `docs/memory/technical-principles.md`

## 🔗 详细记忆库索引

详见 `docs/memory/README.md`

## 📖 参考资源：Coop-Mod

**Coop-Mod 源码位于 `GameSource/Escape-From-Duckov-Coop-Mod-Preview-master/` 目录中。**

当遇到以下问题时，参考 Coop-Mod 的实现：
- 数据持久化方式（JsonUtility.ToJson/FromJson）
- 网络同步时物品信息传递（TypeID而非对象引用）
- 与游戏核心系统集成模式

---
**更新时间**: 2025-10-31
**用途**: 跨会话记忆主要入口，新会话必读！
**核心原则**: 源码驱动，禁止猜测



