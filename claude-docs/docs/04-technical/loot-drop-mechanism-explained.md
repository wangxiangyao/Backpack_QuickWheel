# 掉落机制完整解析

## 📚 掉落流程详解

### 1. 触发时机：玩家交互

```
玩家靠近LootBox
  → 按E键交互
  → InteractableLootbox.GetOrCreateInventory()  (InteractableLootbox.cs:86-88)
  → LootBoxLoader.Setup()  (首次打开时)
  → 生成物品到Inventory
  → 显示掉落界面
```

**关键代码** (`InteractableLootbox.cs:85-88`):
```csharp
LootBoxLoader component = lootBox.GetComponent<LootBoxLoader>();
if (component && component.autoSetup)
{
    component.Setup().Forget();  // 首次打开时生成物品
}
```

### 2. 物品生成流程（Setup方法）

基于`LootBoxLoader.cs`的代码结构推断：

```
Setup()开始
  ↓
【步骤1：固定物品】
  如果有 fixedItems 配置
    → 直接生成这些TypeID的物品
    → 添加到Inventory
  ↓
【步骤2：随机物品】
  如果有 tags 配置
    → 根据权重从tags中随机选择一个Tag  (RandomContainer.GetRandom())
    → 创建ItemFilter { requireTags = [选中的Tag] }
    → 调用 ItemAssetsCollection.Search(filter)  ← 这里匹配我们的配件！
    → 返回所有包含该Tag的物品TypeID列表
    → 从列表中随机选择一个TypeID
    → 生成物品实例
    → 添加到Inventory
  ↓
【步骤3：品质随机】
  如果有 qualities 配置
    → 随机设置物品品质
  ↓
Setup()完成，Inventory填充完毕
```

**核心匹配逻辑** (`LootBoxLoader.cs:50-52`):
```csharp
public static int[] Search(ItemFilter filter)
{
    return ItemAssetsCollection.Search(filter);
}
```

**ItemFilter匹配规则**:
```
物品必须满足：
1. 包含filter.requireTags中的所有tag
2. 不包含filter.excludeTags中的任何tag
3. quality在[minQuality, maxQuality]范围内（如果设置）
```

### 3. 物品创建时机

**重要！**物品是在**玩家首次打开箱子**时才创建的，不是场景加载时：

```
场景加载
  → LootBox对象被创建（预制体实例化）
  → LootBox保持关闭状态（没有物品）
  ↓
玩家首次交互
  → Setup()被调用
  → 物品被生成  ← 这一刻才创建物品！
  → Inventory被填充
  ↓
玩家再次交互
  → 直接显示已生成的Inventory
  → 不再重新生成
```

**持久化机制**:
- 每个LootBox的Inventory会被缓存在`LevelManager.LootBoxInventories`字典中
- Key基于LootBox的位置坐标生成（`InteractableLootbox.cs:94-102`）
- 一旦生成，物品列表不会改变（除非玩家拾取）

## 🎯 为什么配件需要Accessory标签

### 问题示例

假设某个LootBox配置：
```json
{
  "tags": {
    "entries": [
      { "value": "Accessory", "weight": "3" }
    ]
  }
}
```

**掉落流程**:
```
1. 玩家打开箱子
2. RandomContainer.GetRandom() → 随机到"Accessory"
3. 创建 ItemFilter { requireTags = [Accessory] }
4. ItemAssetsCollection.Search(filter)
   ↓
   遍历所有物品，检查：
   - 物品A: Tags = [SidePocket_Small]  ← ❌ 不包含Accessory，排除
   - 物品B: Tags = [SidePocket_Small, Accessory]  ← ✅ 包含Accessory，匹配！
   - 物品C: Tags = [Tool]  ← ❌ 不包含Accessory，排除
   ↓
5. 返回匹配列表 [物品B的TypeID]
6. 从列表中随机选择 → 生成物品B
```

**如果我们的配件没有Accessory标签**:
- Search()返回空列表 `[]`
- 无法生成任何物品
- 配件永远不会掉落

## 📊 Tag权重统计分析

从`GameSource/LootBoxDebug.json`统计：

| Tag | 最高权重 | 出现次数 | 掉落概率 | 适合测试 |
|-----|---------|----------|----------|----------|
| **Bullet** | **90** | 9个箱子 | **极高** | ⭐⭐⭐⭐⭐ 最推荐 |
| **Electric** | 65 | 14个箱子 | 高 | ⭐⭐⭐⭐ |
| **Tool** | 60 | 33个箱子 | 高 | ⭐⭐⭐⭐ |
| **Daily** | 50 | 14个箱子 | 中高 | ⭐⭐⭐ |
| Tool | 35 | 14个箱子 | 中 | ⭐⭐ |
| Accessory | 10 | 1个箱子 | 低 | ⭐ |
| Accessory | 3 | 9个箱子 | 很低 | ⭐ 不适合快速测试 |

**权重计算示例**:
```
箱子配置:
- Bullet: 90
- Accessory: 3
- Explosive: 5
总权重 = 90 + 3 + 5 = 98

掉落概率:
- Bullet: 90/98 = 91.8%  ← 几乎每次都是子弹
- Accessory: 3/98 = 3.1%  ← 需要开30+个箱子才能稳定触发
- Explosive: 5/98 = 5.1%
```

## 🧪 测试方案

### 方案1：使用高权重Tag测试（推荐）

**目标**: 快速验证配件是否能正常被ItemAssetsCollection.Search()找到

**步骤**:
1. **临时添加Bullet标签**到配件
2. 找一个包含Bullet(90%)的箱子
3. 开箱测试
4. 验证成功后恢复为Accessory标签

**代码修改** (临时测试用):
```csharp
// src/AttachmentSystem/AttachmentManager.cs:256-280
private void SetAttachmentTag(Item item, string requiredTag)
{
    // 添加自定义的配件类型标签
    if (createdTags.TryGetValue(requiredTag, out Tag attachmentTag))
    {
        item.Tags.Add(attachmentTag);
    }

    // 🧪 测试用：添加Bullet标签（权重90%，极易触发）
    Tag bulletTag = FindSystemTag("Bullet");
    if (bulletTag != null && !item.Tags.Contains(bulletTag))
    {
        item.Tags.Add(bulletTag);
        Debug.Log($"⚠️ 测试模式：为物品 {item.DisplayName} 添加Bullet标签");
    }

    // 正式版：添加Accessory标签
    Tag accessoryTag = FindSystemTag("Accessory");
    if (accessoryTag != null && !item.Tags.Contains(accessoryTag))
    {
        item.Tags.Add(accessoryTag);
        Debug.Log($"为物品 {item.DisplayName} 添加Accessory标签");
    }
}
```

**测试箱子位置** (Bullet权重90%的箱子):
```
LootBox_Bullet_15: (324.89, 0.00, 192.50)
LootBox_Bullet_25: 其他多个位置
```

**预期结果**:
- 开箱91.8%概率获得配件（因为Bullet权重90%）
- 开1-3个箱子就能触发

### 方案2：使用F8调试工具验证（最快）

**优势**: 不需要实际开箱，直接验证tag配置

**步骤**:
1. 启动游戏
2. 进入场景
3. 按F8
4. 查看控制台输出

**预期输出**:
```
搜索结果: 找到 XX 个物品
  - 网兜 (TypeID: 349100, Quality: 2)
    标签: [SidePocket_Small, Bullet, Accessory]  ← Bullet用于测试
  包含Accessory标签: ✅ 是
```

### 方案3：实际掉落测试（生产验证）

**目标**: 验证真实游戏中的掉落

**步骤**:
1. 移除测试用的Bullet标签
2. 只保留Accessory标签
3. 找包含Accessory的箱子测试

**注意事项**:
- Accessory权重3%，需要开**20-30个箱子**才能稳定触发
- 建议先用方案1快速验证，再切换到生产配置

## 📝 代码修改总结

**当前已修改** (`src/AttachmentSystem/AttachmentManager.cs:256-280`):
```csharp
private void SetAttachmentTag(Item item, string requiredTag)
{
    // 1. 添加自定义tag（背包插槽识别）
    if (createdTags.TryGetValue(requiredTag, out Tag attachmentTag))
    {
        item.Tags.Add(attachmentTag);
    }

    // 2. 添加官方Accessory tag（掉落系统识别）
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

**这个修改确保**:
- ✅ 配件有自己的类型tag（SidePocket_Small等）
- ✅ 配件同时有官方Accessory tag
- ✅ ItemAssetsCollection.Search()能找到配件
- ✅ LootBox能正常掉落配件

## 🔍 调试技巧

### 验证物品tag的方法

**方法1：F8调试工具**
```
按F8 → 查看 "包含Accessory标签: ✅ 是"
```

**方法2：游戏内检查**
```csharp
// 在游戏运行时
var item = ItemAssetsCollection.GetPrefab(349100);
Debug.Log($"Tags: {string.Join(", ", item.Tags.Select(t => t.name))}");
```

**方法3：测试Search功能**
```csharp
var filter = new ItemFilter { requireTags = new Tag[] { accessoryTag } };
var results = ItemAssetsCollection.Search(filter);
Debug.Log($"找到 {results.Length} 个配件");
```

### 快速测试循环

```
1. 修改代码（添加tag）
2. 编译MOD
3. 启动游戏
4. 按F8验证tag  ← 30秒快速验证
5. （可选）开箱测试  ← 完整验证
```

## 📌 常见问题

### Q: 为什么不直接修改lootbox的tag配置？
**A**: Lootbox是官方场景预制体，我们MOD无法修改。只能通过给物品添加tag来适配官方的掉落系统。

### Q: 添加Bullet标签会导致什么问题？
**A**: 配件会从所有Bullet箱子掉落，破坏游戏平衡。**仅用于测试，不要发布**。

### Q: 为什么Accessory权重这么低？
**A**: 官方设计如此。配件是稀有物品，低掉落率符合游戏设计。

### Q: 可以提高Accessory的权重吗？
**A**: 不能，权重在lootbox预制体中配置，MOD无法修改。

## 更新历史

- 2025-11-01: 创建文档，完整解析掉落机制
