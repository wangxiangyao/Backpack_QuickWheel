# 配件掉落功能验证指南

## 📚 相关文档

**建议阅读顺序**：
1. 📖 **[掉落机制完整解析](./loot-drop-mechanism-explained.md)** - 理解掉落流程和触发时机
2. 📄 本文档 - 验证配件掉落功能
3. 🧪 **[测试配置：Bullet标签](./testing-config-bullet-tag.md)** - 快速测试方案（开发用）

## 问题背景

自定义配件无法从lootbox掉落的原因：
- 官方lootbox预制体配置了`"Accessory"` tag作为掉落筛选条件
- 自定义配件只添加了自己的类型tag（如`SidePocket_Small`），没有添加官方的`"Accessory"` tag
- 掉落检查时无法匹配，导致配件永远不会掉落

**详细流程说明**：参见 [掉落机制完整解析](./loot-drop-mechanism-explained.md)

## 解决方案

**代码修改位置**：`src/AttachmentSystem/AttachmentManager.cs:256-280`

**修改内容**：在`SetAttachmentTag`方法中，为每个配件同时添加两个tag：
1. **自定义tag**（如`SidePocket_Small`）- 用于背包插槽识别
2. **官方tag**（`Accessory`）- 用于掉落系统识别

```csharp
private void SetAttachmentTag(Item item, string requiredTag)
{
    // 添加自定义的配件类型标签（用于背包插槽识别）
    if (createdTags.TryGetValue(requiredTag, out Tag attachmentTag))
    {
        item.Tags.Add(attachmentTag);
    }

    // 添加官方的"Accessory"标签（用于掉落系统识别）
    Tag accessoryTag = FindSystemTag("Accessory");
    if (accessoryTag != null)
    {
        if (!item.Tags.Contains(accessoryTag))
        {
            item.Tags.Add(accessoryTag);
        }
    }
}
```

## 掉落验证链条

```
游戏场景中的LootBox
  → 配置的tags (如: "Accessory", "Bullet", "Tool")
  → ItemFilter.requireTags = [Accessory]
  → ItemAssetsCollection.Search(filter)
  → 返回所有包含"Accessory"标签的物品
  → 从返回结果中随机选择
  → 生成物品实例
```

## 验证方法

### 🚀 快速测试方案（开发推荐）

**如果你想快速验证掉落功能**（1-3次开箱），请使用：
👉 **[测试配置：Bullet标签快速验证](./testing-config-bullet-tag.md)**

- ⚡ 权重90%，极易触发
- 🎯 1-3个箱子即可验证
- ⚠️ 仅用于开发测试，不要发布

### 方法1：使用内置调试工具（生产推荐）

项目已经包含了完整的验证工具：`LootDebugManager`

**使用步骤**：
1. 启动游戏并加载MOD
2. 进入游戏世界
3. 按下 `F8` 快捷键
4. 查看Unity控制台输出

**预期输出**：
```
[LootDebugManager] === 开始标签测试分析 ===

--- 1. 测试ItemAssetsCollection.Search() ---
找到Accessory标签: Accessory
开始搜索带有Accessory标签的物品...
搜索结果: 找到 XX 个物品
  - 网兜 (TypeID: 349100, Quality: 2)
    标签: [SidePocket_Small, Accessory]
  - 水壶袋 (TypeID: 349101, Quality: 3)
    标签: [SidePocket_Small, Accessory]
  ...

--- 2. 测试我们创建的配件物品 ---
✅ 标准方式找到新物品: 网兜 (TypeID: 349100)
详细测试: 网兜 (TypeID: 349100)
  标签: [SidePocket_Small, Accessory]
  包含Accessory标签: ✅ 是
  插槽数量: 1

--- 3. 测试标签系统 ---
✅ 找到标签 'Accessory': 1 个实例
```

**关键验证点**：
- ✅ 搜索结果中包含我们的自定义配件
- ✅ 每个配件都有`Accessory`标签
- ✅ "包含Accessory标签: ✅ 是"

### 方法2：实际掉落测试

**测试位置**（基于LootBoxDebug.json）：

包含"Accessory"标签的lootbox位置示例：
- `LootBox_Bullet_15` - 位置: (324.89, 0.00, 192.50)
  - Tags: Explosive(5%), Bullet(90%), Accessory(3%)
- `LootBox_Tools3_15` - 位置: (314.87, -0.03, 279.91)
  - Tags: Tool(60%), Continer(5%)
- `LootBox_Normal_1` - 位置: (405.08, 4.05, 157.25)
  - Tags: Daily(50%), Tool(35%), Electric(65%)

**测试步骤**：
1. 找到包含"Accessory"标签的lootbox
2. 多次开箱（由于权重较低3%，可能需要多次尝试）
3. 检查是否获得自定义配件

**注意**：
- Accessory的掉落权重通常很低（3%-10%）
- 可能需要开启多个箱子才能触发
- 某些lootbox可能没有Accessory标签配置

### 方法3：代码验证（开发者模式）

如果需要强制验证，可以临时修改权重：

**临时修改建议**（仅用于测试，不要提交）：
```csharp
// 在测试环境中临时提高掉落权重
// 修改某个lootbox的RandomContainer配置
// 将Accessory权重从3提高到90
```

## 已知的包含"Accessory"的Lootbox

从`GameSource/LootBoxDebug.json`中提取的统计：

| Lootbox名称 | 位置坐标 | Accessory权重 | 其他tags |
|------------|----------|---------------|----------|
| LootBox_Bullet_15 | (324.89, 0.00, 192.50) | 3% | Explosive(5%), Bullet(90%) |
| LootBox_Bullet_* | 多个位置 | 3% | Explosive(5%), Bullet(90%) |
| [其他箱子] | 见json文件 | 3%-10% | 各种组合 |

## 故障排查

### 问题1：F8调试输出显示"未找到Accessory标签"
**原因**：系统tag加载失败
**解决**：检查`LoadSystemTags()`是否正常执行

### 问题2：配件有Accessory标签但仍不掉落
**原因**：可能是权重太低或测试次数不够
**解决**：
1. 增加测试次数（至少开20个包含Accessory的箱子）
2. 检查lootbox的randomPool配置是否正确

### 问题3：验证工具找到配件但游戏中无法获得
**原因**：可能是lootbox的quality筛选限制
**解决**：检查`ItemFilter.minQuality`和`maxQuality`设置

## 相关文件

- `src/AttachmentSystem/AttachmentManager.cs` - 配件创建和tag设置
- `src/LootDebugSystem/LootDebugManager.cs` - 掉落验证工具
- `GameSource/LootBoxDebug.json` - 所有lootbox配置信息
- `src/BackpackModConfig.cs` - 配件物品配置定义

## 更新日期

2025-11-01
