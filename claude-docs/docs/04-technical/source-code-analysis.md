# 源码分析记录

## 🎯 目标
理解官方掉落和商店系统，验证新配件的掉落和购买配置

## 📁 已理解文件清单

### 1. **LootBoxLoader.cs** - 掉落箱控制器
- **作用**: 控制游戏世界中掉落箱的物品生成
- **核心方法**:
  - `Setup()` - 设置掉落物品
  - `RandomActive()` - 决定掉落箱是否激活
  - `CreateCash()` - 生成现金
- **配置字段**:
  - `randomPool` - 随机物品池 (RandomContainer<Entry>)
  - `fixedItems` - 固定掉落物品列表
  - `tags` - 标签过滤池 (RandomContainer<Tag>)
  - `randomFromPool` - 是否从标签池随机
  - `excludeTags` - 排除标签
- **关键发现**: 有4种掉落方式：随机池、标签过滤、固定物品、动态搜索

### 2. **InteractableLootbox.cs** - 掉落箱交互
- **作用**: 玩家与掉落箱交互的界面逻辑
- **核心方法**:
  - `CreateFromItem()` - 从物品创建掉落箱（死亡掉落）
  - `GetOrCreateInventory()` - 创建或获取背包库存
- **关键发现**: 死亡掉落机制，从物品插槽和背包内容生成掉落物

### 3. **ItemAssetsCollection.cs** - 物品资源管理
- **作用**: 管理所有物品的预制体和注册
- **核心方法**:
  - `GetPrefab(int typeID)` - 根据TypeID获取物品预制体
  - `Search(ItemFilter filter)` - 根据过滤器搜索物品
  - `AddDynamicEntry(Item item)` - 动态注册新物品
- **关键发现**: AdditionalCollectibles Mod使用此方法注册新物品

### 4. **TradingUIUtilities.cs** - 商店UI工具
- **作用**: 商店系统的UI管理
- **核心属性**: `ActiveMerchant` - 当前活跃商人
- **关键发现**: 商人系统通过IMerchant接口实现价格转换

### 5. **IMerchant.cs** - 商人接口
- **作用**: 定义商人行为规范
- **核心方法**: `ConvertPrice(Item item, bool selling)` - 价格转换
- **关键发现**: 价格可以通过商人动态调整

### 6. **AdditionalCollectibles.cs** - 额外收集品Mod
- **作用**: 参考实现，展示如何添加新物品到游戏
- **核心流程**:
  1. 克隆现有物品: `ItemAssetsCollection.GetPrefab(config.OriginalItemId)`
  2. 修改物品属性: `SetItemProperties(item, config)`
  3. 设置Tags: `item.Tags.Add(this.GetTargetTag(tagName))`
  4. 动态注册: `ItemAssetsCollection.AddDynamicEntry(item)`
- **关键发现**:
  - 新物品必须设置合适的Tag才能正常工作
  - 使用`AddDynamicEntry`注册到物品系统
  - **没有修改官方掉落表**

### 7. **BackpackModConfig.cs** - 我们的Mod配置
- **作用**: 定义所有背包配件的配置
- **核心数据**: `AttachmentItemConfigs` - 配件物品配置列表
- **关键问题**: 配件**没有设置Tag**，只有插槽配置

## 🔍 关键发现 - 物品掉落的核心机制

### ItemAssetsCollection.Search() 方法详解
```csharp
public static int[] Search(ItemFilter filter)
{
    // 1. 检查缓存
    if (cachedSearchResults.TryGetValue(filter.GetHashCode(), out result))
        return result;

    // 2. 获取匹配的TypeID
    result = GetAllTypeIds(filter);

    // 3. 如果没有结果，降低品质要求重新搜索
    while (result.Length < 1)
    {
        DownGradeSearch(ref filter); // 降低maxQuality和minQuality
        if (filter.maxQuality < 0 || filter.minQuality < 0) break;
    }

    // 4. 缓存结果
    cachedSearchResults[filter.GetHashCode()] = result;
    return result;
}
```

### GetAllTypeIds() 核心逻辑
```csharp
// 搜索官方物品 + 动态物品
IEnumerable<int> collection = from e in Instance.entries.FindAll(entry =>
        EvaluateFilter(entry.metaData, filter)) select e.typeID;
IEnumerable<int> range = from e in dynamicDic.Where(e =>
        e.Value.prefab != null && EvaluateFilter(e.Value.MetaData, filter))
        select e.Key;
```

**关键发现**：
1. **同时搜索官方物品和动态物品** - 我们的动态注册物品会被包含
2. **基于ItemFilter过滤** - Tag、品质、口径等条件
3. **自动降级机制** - 如果高品质没找到，会降低品质重新搜索

## ❓ 待深入理解文件

### 需要找到的关键组件：
1. **ItemFilter类的完整定义** - 在ItemStatsSystem命名空间中
2. **EvaluateFilter函数实现** - 过滤条件的具体逻辑
3. **RandomContainer实现** - 随机选择机制
4. **LootBoxLoader.Setup()的完整实现** - 如何使用Search方法

## 🔍 下一步分析策略

1. **找到ItemFilter的完整定义** - 理解过滤条件
2. **查看我们的配件注册方式** - 确认Tag设置
3. **对比AdditionalCollectibles和我们的配置差异**
4. **测试配件是否能被ItemFilter正确匹配**