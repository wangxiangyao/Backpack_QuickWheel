# Duckov Backpack Mod - 项目记忆档案

## 项目概述
Unity Mod开发项目，为游戏《逃离鸭科夫》添加背包配件系统，支持嵌套插槽功能。

## 项目结构总览

### 核心目录
- **GameSource/Duckov/** - 游戏官方源码（最高优先级）
  - Inventory.cs - 背包/物品容器系统
  - Item.cs - 物品基类
  - ItemAssetsCollection.cs - 物品资源集合和获取
- **AttachmentUI/** - 配件UI系统
- **VoiceWheelSystem/** - 语音轮盘系统
- **Patches/** - Harmony补丁
- **BackpackModConfig.cs** - 配件配置
- **docs/** - 文档目录

### 参考资源
- **GameSource/Escape-From-Duckov-Coop-Mod-Preview-master/** - Coop-Mod源码参考
  - 数据持久化模式
  - 网络同步物品信息传递方式
  - 与游戏核心系统集成模式

## 关键技术细节

### Item类使用规范 ⚠️
```csharp
// ✅ 正确
int count = target.Slots.Count;  // SlotCollection使用Count

// ❌ 错误
int count = target.Slots.Length; // 编译错误！Slots不是数组
```

### Unity操作规范
```csharp
// ✅ 正确的反射操作
if (_slotIndicatorContainerField == null) return;
GameObject container = _slotIndicatorContainerField.GetValue(__instance) as GameObject;
if (container == null) return;

// ✅ 正确的对象销毁
Object.DestroyImmediate(oldLayout); // 同步删除
var gridLayout = container.AddComponent<GridLayoutGroup>();
if (gridLayout == null) return; // 防守检查
```

### 气泡速度参数 ⚡
- **位置**: GameSource/Duckov/DialogueBubble.cs:137
- **参数**: `defaultSpeed = 10f`
- **含义**: speed数值**越大越快**
- **最佳实践**: `playerCharacter.PopText(text, 50f)` // 5倍默认速度

## 开发原则（最高优先级）

### 1. 源码驱动开发原则 ⚠️ 【铁律】
```
问题复现 → 查找GameSource/Duckov/源码 → 分析参数含义 → 基于源码解决 → 验证效果
```

### 2. 禁止主观臆测 🚫
- ❌ 基于参数名称猜测功能
- ❌ 基于经验实现功能
- ✅ 必须查看官方源码确认

### 3. 官方API优先 ✅
- 优先使用游戏官方API
- 避免重复造轮子
- 保证兼容性和性能

### 4. 简洁性原则 🎯
- 避免过度复杂实现
- 不用协程、反射、多层抽象（除非必要）

### 5. 参数验证流程 🔬
1. 查看源码中参数定义和注释
2. 小范围实验测试
3. 记录参数效果和最优值
4. 代码注释说明参数含义

## 文件位置记忆

### 任务管理
- **唯一任务文件**: docs/TODO-待做清单.md

### 项目文档
- **项目结构**: docs/PROJECT_STRUCTURE-项目结构总结.md
- **项目记忆**: docs/project-memory.md (当前文件)

### 源码位置
- **游戏源码**: GameSource/Duckov/
- **Mod配置**: BackpackModConfig.cs

## 检索模式

### 物品相关查询
```
搜索目标: GameSource/Duckov/Item*.cs
关键词: Inventory, Item, ItemAssetsCollection
```

### UI交互查询
```
搜索目标: ItemDisplay.cs + AttachmentUI/
关键词: UI, Display, Interface
```

### 配置修改查询
```
搜索目标: BackpackModConfig.cs
关键词: Config, Settings, Configuration
```

### 任务列表查询
```
搜索目标: docs/TODO-待做清单.md
关键词: TODO, 任务, 待办
```

## 成功案例记录

### 语音轮盘气泡优化 (2025-10-31)
- **问题**: 气泡显示响应慢，位置错误
- **解决**: 研究源码发现 `CharacterMainControl.PopText(text, 50f)`
- **学习**: 源码研究价值和官方API威力
- **文档**: docs/reflections/speech-wheel-bubble-optimization-reflection.md

## 数据模式参考（Coop-Mod）

### 序列化方式
```csharp
JsonUtility.ToJson/FromJson // 数据持久化
```

### 物品标识
```csharp
TypeID // 标识物品，避免对象引用
```

### 数据管理
```csharp
Saves系统 // 管理数据持久化
```

---
**生成时间**: 2025-10-31
**项目状态**: 活跃开发中
**关键原则**: 源码驱动，禁止猜测