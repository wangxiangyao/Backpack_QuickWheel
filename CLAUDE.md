# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此代码库中工作时提供指导。

## 项目概述

这是一个为游戏《逃离鸭科夫》开发的 Unity Mod，添加了背包配件系统。该 Mod 允许玩家为现有背包添加配件插槽，并创建各种配件物品（侧面包、战术包、子弹袋等），支持嵌套插槽功能（配件中还有配件）。


## 重要提示：源码位置

**所有游戏官方源码都位于 `GameSource/Duckov/` 目录中。** 包括但不限于：
- `Inventory.cs` - 背包/物品容器系统
- `Item.cs` - 物品基类
- `ItemAssetsCollection.cs` - 物品资源集合和获取
- 其他系统源码等

在阅读源码或寻找关键实现时，**总是先在 `GameSource/Duckov/` 中查找**。

## 参考资源：Coop-Mod

**Coop-Mod 源码位于 `GameSource/Escape-From-Duckov-Coop-Mod-Preview-master/` 目录中。**

当遇到以下问题时，参考 Coop-Mod 的实现：
- 数据持久化方式（如何保存和加载自定义数据）
- 网络同步时如何传递物品信息（通常是TypeID而非对象引用）
- 与游戏核心系统的交互模式

Coop-Mod 的开发者已经解决了许多与游戏系统集成的问题，他们的实现模式值得参考。特别是：
- 使用 `JsonUtility.ToJson/FromJson` 进行数据序列化
- 使用 TypeID 来标识物品而非对象引用
- 利用游戏的 Saves 系统来管理数据持久化


### Item类使用注意事项 ⚠️

**常见错误与修复**：

❌ **错误1：使用 `.Length` 访问 Item.Slots**
```csharp
int count = target.Slots.Length;  // ❌ 编译错误！Slots是SlotCollection，没有Length
```

✅ **正确做法**：
```csharp
int count = target.Slots.Count;  // ✅ 使用Count属性
```

❌ **错误2：反射获取字段后不做null检查**
```csharp
GameObject container = _slotIndicatorContainerField.GetValue(__instance) as GameObject;
// 直接使用container，可能导致NullReferenceException
```

✅ **正确做法**：
```csharp
if (_slotIndicatorContainerField == null) return;
GameObject container = _slotIndicatorContainerField.GetValue(__instance) as GameObject;
if (container == null) return;
```

❌ **错误3：Destroy后立即操作同一对象**
```csharp
Object.Destroy(oldLayout);  // 异步删除
var gridLayout = container.AddComponent<GridLayoutGroup>();  // 可能失败
```

✅ **正确做法**：
```csharp
Object.DestroyImmediate(oldLayout);  // 同步删除
var gridLayout = container.AddComponent<GridLayoutGroup>();  // 确保成功
if (gridLayout == null) return;  // 防守检查
```

**关键规则**：
- Item.Slots 的类型是 `SlotCollection`，不是数组
- 必须使用 `.Count` 获取数量，不能用 `.Length`
- 所有反射操作必须有 null 检查
- 涉及Unity对象创建/销毁时，使用 try-catch 包裹关键代码
- 对于ADD/GET/REMOVE组件操作，使用DestroyImmediate确保同步


## 开发交流规则

### Git 提交规则
- ❌ 不要在提交信息中写入AI助手的名字（如"Generated with Claude Code"）
- ✓ 只提交干净的技术信息，专注于代码变更本身

### 问题解决原则

## 🔧 Unity Mod 开发故障排除核心原则

### 1. 源码驱动开发原则 ⚠️ **【最高优先级】**
**核心规则**：任何Unity系统相关的问题，必须先查看官方源码！

❌ **致命错误**：基于参数名称或经验猜测功能
```csharp
// ❌ 错误：认为speed数值越小越快
playerCharacter.PopText(text, 0.1f);  // 实际结果是超级慢！
```

✅ **正确做法**：必须查看官方源码确认参数真实含义
```csharp
// ✅ 正确：查看DialogueBubble.cs源码后发现speed数值越大越快
playerCharacter.PopText(text, 50f);  // 基于源码理解的正确设置
```

### 2. 关键参数验证铁律 ⚡
**实际案例记录**：
- **发现位置**：`GameSource/Duckov/DialogueBubble.cs:137`
- **源码真相**：`private float defaultSpeed = 10f;`
- **参数含义**：speed控制气泡出现动画速度，**数值越大越快**
- **错误理解**：0.1f = 更快 ❌ （实际是超级慢）
- **正确使用**：50f = 超快 ✅ （5倍默认速度）

### 3. 源码查找优先级
1. **第一优先级**：查看 `GameSource/Duckov/` 中的官方源码
2. **第二优先级**：参考 Coop-Mod 实现模式
3. **绝对禁止**：基于经验或参数名称臆测功能

### 4. 中文交流优势法则
- **鼓励母语交流**：中文用户使用中文描述技术需求更准确
- **避免理解偏差**：英文交流容易导致技术细节误解
- **精确表达**：复杂问题用中文更容易描述清楚

### 5. 标准故障排除流程
```
问题复现 → 查找相关源码 → 分析参数含义 → 基于源码解决问题 → 验证效果
```

### 补充原则
3. **官方API优先使用** ✅：优先使用游戏官方提供的API和机制，避免重复造轮子。官方API保证兼容性和性能。
4. **避免主观臆测** 🚫：**永远不要基于猜测实现功能**。不理解官方实现时，必须先研究源码或请求更多源码支持。
5. **性能优先机制** ⚡：优先使用事件监听等高效机制，避免轮询检查等性能消耗大的实现。

### 技术实现准则
1. **官方API优先** ✅：优先使用游戏官方提供的API和机制，避免自定义实现。如：`CharacterMainControl.PopText(text, 50f)` 而非自定义气泡系统。
2. **简洁性原则** 🎯：避免过度复杂的实现。如果官方API能解决问题，就不要创建复杂的协程、反射或多层抽象。
3. **参数验证流程** 🔬：使用任何参数前必须：
   - 查看源码中参数定义和注释
   - 进行小范围实验测试
   - 记录参数效果和最优值
   - 在代码注释中说明参数含义
4. **递归检测**：所有可能递归调用的地方必须添加递归检测标志
5. **物品有效性检查**：使用 `IsBeingDestroyed` 和 `ParentItem != null` 确保物品状态
6. **UI刷新机制**：确保物品状态变化时UI及时更新，避免显示不一致

### 开发流程标准
1. **源码研究阶段** 📖：
   - 查看相关官方源码（`GameSource/Duckov/`）
   - 理解官方实现机制和推荐做法
   - 确认可用的官方API和参数含义

2. **设计阶段** 🎨：
   - 基于官方API设计方案
   - 优先选择官方推荐的做法
   - 避免重复实现现有功能

3. **实现阶段** 💻：
   - 使用官方API而非自定义实现
   - 遵循简洁性原则
   - 添加详细的调试日志

4. **验证阶段** ✅：
   - 测试参数效果和性能
   - 验证与游戏系统的兼容性
   - 记录最佳实践和参数配置

### 调试与测试
1. **分步测试**：每次修改后进行实际游戏测试，验证功能正常
2. **日志输出**：关键操作添加详细日志，便于问题定位
3. **性能监控**：关注性能影响，避免死循环和内存泄漏

## 成功案例参考

### 语音轮盘气泡显示优化 (2025-10-31)
**问题**: 语音气泡显示响应慢，显示位置错误
**解决方案**: 通过研究源码发现 `CharacterMainControl.PopText(text, 50f)` 官方API
**关键学习**: 源码研究的价值和官方API的威力
**详细记录**: `docs/reflections/speech-wheel-bubble-optimization-reflection.md`

**最佳实践**:
```csharp
// ✅ 基于源码研究的最佳实践
playerCharacter.PopText(text, 50f);  // 50f = 超快速度（源码确认）

// ❌ 避免: 复杂的自定义实现
// 协程、反射、多层抽象等不必要的复杂性
```

