# 项目结构总结 - 逃离鸭科夫背包Mod

## 📁 项目根目录结构

```
Great_backpack/
├── GameSource/                    # 🎯 游戏官方源码（最高优先级）
│   ├── Duckov/                   # 游戏核心源码
│   │   ├── Item.cs               # 物品基类
│   │   ├── Inventory.cs          # 背包/物品容器系统
│   │   ├── ItemAssetsCollection.cs # 物品资源集合和获取
│   │   ├── ItemDisplay.cs        # 物品显示和交互 (487-530: OnPointerClick)
│   │   ├── DialogueBubble.cs     # 对话气泡系统 (137: defaultSpeed = 10f)
│   │   ├── CharacterMainControl.cs # 角色主控制 (PopText方法)
│   │   ├── ItemShortcutPanel.cs  # 快捷栏管理
│   │   └── ...                   # 其他游戏系统源码
│   └── Escape-From-Duckov-Coop-Mod-Preview-master/ # Coop-Mod参考源码
│       ├── 数据持久化模式参考
│       ├── 网络同步机制参考
│       └── 游戏系统集成模式参考
│
├── AttachmentUI/                 # 🔧 背包配件UI系统
│   └── SlotIndicatorCacheManager.cs # 拖拽高亮管理 (关键: rulesPerFrame = 2)
│
├── VoiceWheelSystem/             # 🎵 语音轮盘系统（开发中）
│   └── VoiceWheelManager.cs      # 语音轮盘主控制器
│
├── Patches/                      # 🛠️ Harmony补丁目录
│   ├── ItemUIUtilities.cs        # UI工具类 (NotifyPutItem方法)
│   └── ...                       # 其他补丁文件
│
├── BackpackModConfig.cs          # ⚙️ 配件配置文件 (141行: GagaTactical_Item配置)
│
├── docs/                         # 📚 项目文档
│   ├── TODO-待做清单.md          # 🎯 唯一的任务管理文件
│   ├── PROJECT_STRUCTURE-项目结构总结.md # 本文件
│   ├── PLANNING-开发计划/         # 开发计划文档
│   ├── UI_FEATURES-UI功能计划.md  # UI功能计划
│   ├── CHANGELOG-更新日志.md      # 更新日志
│   └── ARCHITECTURE-架构说明.md   # 架构说明
│
└── Assembly-CSharp.csproj        # 项目文件
```

## 🎯 核心系统文件位置

### 1. 游戏官方源码（GameSource/Duckov/）
- **Item.cs**: 物品基类，所有物品的基础
- **Inventory.cs**: 背包容器系统
- **ItemDisplay.cs**: 物品交互处理
- **ItemAssetsCollection.cs**: 物品资源管理
- **DialogueBubble.cs**: 气泡显示系统
- **CharacterMainControl.cs**: 角色控制

### 2. Mod核心文件
- **BackpackModConfig.cs**: 配件配置中心
- **AttachmentUI/SlotIndicatorCacheManager.cs**: 拖拽高亮系统
- **VoiceWheelSystem/VoiceWheelManager.cs**: 语音轮盘控制

### 3. Harmony补丁（Patches/）
- **ItemUIUtilities.cs**: UI工具和通知系统

## 🔧 关键技术细节

### Item类使用注意事项 ⚠️
```csharp
// ❌ 错误：Slots是SlotCollection，没有Length
int count = item.Slots.Length;  // 编译错误！

// ✅ 正确：使用Count属性
int count = item.Slots.Count;
```

### 反射操作安全规则
```csharp
// 必须进行null检查
if (_slotIndicatorContainerField == null) return;
GameObject container = _slotIndicatorContainerField.GetValue(__instance) as GameObject;
if (container == null) return;
```

### Unity对象操作规则
```csharp
// 使用DestroyImmediate确保同步删除
Object.DestroyImmediate(oldLayout);  // ✅ 同步删除
// Object.Destroy(oldLayout);        // ❌ 异步删除，可能导致后续操作失败
```

### 气泡速度参数（基于源码研究）
```csharp
// DialogueBubble.cs:137 - defaultSpeed = 10f
// speed数值越大越快，不是越小越快！
playerCharacter.PopText(text, 50f);  // 50f = 超快（5倍默认速度）
```

## 📋 开发流程标准

### 1. 源码驱动开发原则 ⚠️ **【最高优先级】**
- **第一步**：查看 `GameSource/Duckov/` 中的官方源码
- **第二步**：理解官方实现机制和参数含义
- **第三步**：基于官方API设计方案
- **绝对禁止**：基于经验或参数名称臆测功能

### 2. 问题解决标准流程
```
问题复现 → 查找相关源码 → 分析参数含义 → 基于源码解决问题 → 验证效果
```

### 3. Coop-Mod参考模式
- 数据持久化：`JsonUtility.ToJson/FromJson`
- 网络同步：使用TypeID而非对象引用
- 系统集成：利用游戏的Saves系统

## 🧠 记忆检索指南

### 快速查找关键词
- **任务列表**：`docs/TODO-待做清单.md`
- **源码位置**：`GameSource/Duckov/`
- **配置文件**：`BackpackModConfig.cs`
- **UI系统**：`AttachmentUI/`
- **补丁文件**：`Patches/`
- **文档总览**：`docs/PROJECT_STRUCTURE-项目结构总结.md`

### 关键模式识别
- **物品相关** → GameSource/Duckov/Item*.cs
- **UI交互** → ItemDisplay.cs + AttachmentUI/
- **配置修改** → BackpackModConfig.cs
- **数据持久化** → 参考Coop-Mod模式
- **气泡显示** → DialogueBubble.cs + CharacterMainControl.cs

## 📝 文档更新规则
- 唯一任务文件：`docs/TODO-待做清单.md`
- 项目结构：本文档为唯一结构参考
- 开发记录：更新到对应CHANGELOG
- 删除临时文件：避免claudedocs、reflections等临时目录

---

**创建时间**：2025-10-31
**更新目的**：建立项目记忆体系，确保快速准确检索
**使用规则**：任何代码修改前先查阅本文档确认文件位置