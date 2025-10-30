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
1. **优先查阅官方源码**：遇到问题时，首先查看GameSource中的游戏源码，理解官方实现逻辑
2. **避免主观猜测**：不要基于假设实现功能，必须基于官方源码的行为
3. **及时请求源码支持**：如果缺少相关源码或无法理解官方实现，立即告知用户需要更多源码
4. **性能优先**：避免轮询检查等性能消耗大的实现，优先使用事件监听等高效机制

### 技术实现准则
1. **依赖官方机制**：尽量使用游戏原有的事件系统和API，避免自定义轮询
2. **递归检测**：所有可能递归调用的地方必须添加递归检测标志
3. **物品有效性检查**：使用 `IsBeingDestroyed` 和 `ParentItem != null` 确保物品状态
4. **UI刷新机制**：确保物品状态变化时UI及时更新，避免显示不一致

### 调试与测试
1. **分步测试**：每次修改后进行实际游戏测试，验证功能正常
2. **日志输出**：关键操作添加详细日志，便于问题定位
3. **性能监控**：关注性能影响，避免死循环和内存泄漏

