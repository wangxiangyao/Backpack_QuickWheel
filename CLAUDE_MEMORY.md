# Claude 项目记忆 - 跨会话检索指南

**⚠️ 新会话开始时必读**: 在开始任何工作前，请先阅读本文档以快速理解项目结构！

## 🎯 项目速览

这是一个《逃离鸭科夫》Unity Mod项目，主要功能是**背包配件系统**，允许为背包添加配件插槽和各种配件物品。

## 📁 核心文件位置（按重要性排序）

### 🔴 游戏官方源码 - **最高优先级**
**位置**: `GameSource/Duckov/`

**关键文件**:
- `Item.cs` - 物品基类
- `Inventory.cs` - 背包容器系统
- `ItemDisplay.cs` - 物品交互 (487-530行: OnPointerClick方法)
- `DialogueBubble.cs` - 气泡系统 (137行: defaultSpeed = 10f)
- `CharacterMainControl.cs` - 角色控制 (PopText方法)
- `ItemShortcutPanel.cs` - 快捷栏管理
- `ItemAssetsCollection.cs` - 物品资源管理

**⚠️ 铁律**: 任何Unity系统问题必须先查看这里！禁止基于经验猜测。

### 🟡 Mod核心文件
- `BackpackModConfig.cs` - 配件配置中心 (141行: GagaTactical_Item配置)
- `AttachmentUI/SlotIndicatorCacheManager.cs` - 拖拽高亮系统
- `VoiceWheelSystem/VoiceWheelManager.cs` - 语音轮盘控制
- `Patches/ItemUIUtilities.cs` - UI工具和通知系统

### 🟢 文档系统
- `docs/TODO-待做清单.md` - **唯一任务文件**
- `docs/PROJECT_STRUCTURE-项目结构总结.md` - 详细结构说明

## 🔧 关键技术细节

### Item类使用 ⚠️
```csharp
// ❌ 错误
int count = item.Slots.Length;  // 编译错误！SlotCollection没有Length

// ✅ 正确
int count = item.Slots.Count;   // 使用Count属性
```

### 反射操作安全
```csharp
// 必须null检查
if (_field == null) return;
GameObject obj = _field.GetValue(instance) as GameObject;
if (obj == null) return;
```

### Unity对象操作
```csharp
Object.DestroyImmediate(obj);  // ✅ 同步删除
// Object.Destroy(obj);        // ❌ 异步，可能导致问题
```

### 气泡速度参数（重要！）
```csharp
// DialogueBubble.cs:137 - defaultSpeed = 10f
// speed数值越大越快！不是越小越快
playerCharacter.PopText(text, 50f);  // 50f = 超快(5倍速)
```

## 🎯 开发原则

1. **源码驱动开发**: 先看GameSource/Duckov/源码，再实现
2. **官方API优先**: 使用游戏现有API，不重复造轮子
3. **Coop-Mod参考**: 数据持久化用JsonUtility，网络同步用TypeID
4. **禁止猜测**: 不理解实现时必须查源码

## 🧠 快速检索模式

当用户提到相关需求时，按以下模式查找：

**物品/背包相关** → `GameSource/Duckov/Item*.cs`
**UI交互问题** → `GameSource/Duckov/ItemDisplay.cs` + `AttachmentUI/`
**配置修改** → `BackpackModConfig.cs`
**拖拽高亮** → `AttachmentUI/SlotIndicatorCacheManager.cs`
**气泡显示** → `GameSource/Duckov/DialogueBubble.cs`
**任务查询** → `docs/TODO-待做清单.md`
**项目结构** → `docs/PROJECT_STRUCTURE-项目结构总结.md`

## 🚨 常见陷阱

1. **参数误解**: speed参数，数值越大越快，不是越小越快
2. **类型错误**: Item.Slots是SlotCollection，用Count不用Length
3. **异步陷阱**: Unity对象删除用DestroyImmediate
4. **反射安全**: 必须做null检查
5. **源码依赖**: 禁止基于经验猜测，必须查源码

## 📋 会话开始检查清单

新会话开始时：
1. ✅ 阅读本文档 (CLAUDE_MEMORY.md)
2. ✅ 了解项目是背包配件Mod
3. ✅ 知道源码在GameSource/Duckov/
4. ✅ 记住技术细节和陷阱
5. ✅ 确认任务文件位置

---

**更新时间**: 2025-10-31
**用途**: 跨会话项目记忆，确保新会话快速上手
**使用规则**: 新会话开始必读！