# CLAUDE.md

**⚠️ 新会话开始必读**: 这是跨会话记忆的主要入口文件！

## 🎯 项目速览

这是一个为游戏《逃离鸭科夫》开发的 Unity Mod，主要功能是**背包配件系统**，允许为背包添加配件插槽和各种配件物品，支持嵌套插槽功能。

## 🏗️ 项目结构（已标准化）

**目录结构**（2025-10-31标准化）:
```
Backpack_QuickWheel/
├── 🧠 claude-docs/              # Claude的主场
│   ├── CLAUDE.md               # 项目记忆入口（当前文件）
│   ├── docs/                   # 项目文档
│   └── memory/                 # 详细经验库
├── 📁 src/                     # 源代码目录
│   ├── AttachmentSystem/       # 配件系统
│   ├── AttachmentUI/           # 配件UI
│   ├── BackpackSystem/         # 背包系统
│   ├── ShortcutSystem/         # 快捷键系统
│   ├── VoiceWheelSystem/       # 语音轮系统
│   └── *.cs                    # 核心源码文件
├── 📁 GameSource/              # 游戏官方源码
└── 📋 其他项目文件/
```

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

### 3. 🔥 **究极准则：信息完整性原则** 【超级最高优先级】
**遇到以下情况必须停止并暴露信息缺口**：
1. **找不到类/方法定义** - ItemFilter, RandomContainer, OnQuackInput等
2. **方法实现被编译隐藏** - 如async状态机，运行时生成的方法
3. **缺少关键配置文件** - 掉落表、商店配置、输入映射等
4. **任何需要假设才能继续的情况**

**🔥 自检三连问（每次思考前必须问自己）**：
1. **我是否基于假设在思考？**
2. **我是否暴露了信息缺口？**
3. **我是否问了用户确认？**

**🔥 允许猜测但不允许基于猜测做决定**：
- ❌ 错误："我认为OnDash可能对应F10，直接实现..."
- ✅ 正确："我猜测OnDash可能对应某个功能键，但不确定。OnDash实际对应哪个键？请确认后再实现。"

### 4. Item类使用规范 ⚠️
```csharp
// ✅ 正确
int count = item.Slots.Count;   // SlotCollection用Count

// ❌ 致命错误
int count = item.Slots.Length;  // 编译错误！
```

### 5. 关键参数实例（气泡速度）⚡
- **位置**: `DialogueBubble.cs:137` - `defaultSpeed = 10f`
- **真相**: speed数值**越大越快**（不是越小越快）
- **正确**: `playerCharacter.PopText(text, 50f)` // 5倍速

### 6. 反射操作安全
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
**任务查询** → `docs/01-core/TODO.md`
**详细记忆** → `docs/memory/` 目录
**文档系统** → `docs/README.md` （文档总览和使用指南）
**文档使用方法** → `docs/memory/documentation-system-guide.md` （详细使用指南）
**关键经验** → `docs/memory/key-experiences.md` （核心经验和最佳实践）

## 🔧 **编译指令（必须记住）**

**项目编译命令**：
```bash
dotnet build src/Backpack_QuickWheel.csproj
```

**输出目录**：
- `D:\steam\steamapps\common\Escape from Duckov\Duckov_Data\Mods\Backpack_QuickWheel\`

**框架配置**：
- .NET Standard 2.1
- 游戏路径通过 `.csproj` 的 `DuckovPath` 变量设置

**⚠️ 已知问题（待解决）**：
- **编译警告** - 大量nullable引用类型警告，可能影响游戏功能
- **缺失依赖** - SodaLocalization程序集缺失（已临时处理）
- **优先级** - 警告问题需要以后找时间解决

## 📋 会话启动检查清单

新会话开始时必须：
1. ✅ 阅读当前文件（CLAUDE.md）
2. ✅ 查看 `docs/README.md` 了解文档系统结构
3. ✅ 知道项目是背包配件Mod
4. ✅ 记住源码在GameSource/Duckov/
5. ✅ 理解源码驱动开发原则
6. ✅ 查看编译指令（dotnet build Backpack_QuickWheel.csproj）
7. ✅ 查看 `docs/01-core/TODO.md` 了解当前任务

**遇到复杂任务时**：
- ✅ 复杂任务识别：>4小时、多文件、新技术？
- ✅ 执行任务开始三重分析（详见 `docs/memory/task-planning-best-practices.md`）
- ✅ 问题理解 → 方案规划 → 任务分解

## 🎯 任务完成守则 【最高优先级】

### 三层持久化方法论
**"每完成一个重要任务，必须完成三层持久化记录，一个都不能少！"**

1. **Git提交记录** - 代码变更历史和可追溯性
2. **项目文档更新** - TODO清单、CHANGELOG状态更新
3. **技术设计文档** - 完整的实现细节和经验总结

**详细说明**: `docs/memory/technical-principles.md`

### 复杂任务规划原则
**"复杂任务开始时，必须进行三重分析：问题理解 → 方案规划 → 任务分解"**

**详细指南**: `docs/memory/task-planning-best-practices.md`

### 元认知核心原则
**"每次完成重要工作或发现宝贵经验时，必须自动进行三重记忆固化：
1. 凝练核心要点到主要记忆入口
2. 详细方法到经验总结文档
3. 使用指导到实践指南文档"**

**详细指南**: `docs/memory/meta-cognition-patterns.md`

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



