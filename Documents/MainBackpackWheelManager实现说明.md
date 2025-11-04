# MainBackpackWheelManager 实现说明

## 📋 概述

MainBackpackWheelManager 是主背包轮盘数据管理器，实现了基于主背包 Inventory 的数据源，支持轮盘与背包位置的双向同步机制。

## 🎯 核心特性

### 1. 数据源特性
- **数据来源**：主角色的主背包 Inventory
- **轮盘容量**：总共8个位置（不是每个类别8个）
- **位置同步**：轮盘与背包位置双向映射
- **物品过滤**：只支持有 Tag 的物品（Healing、Injector、Food、Explosive、MeleeWeapon）

### 2. 智能插入算法
- **顺序映射**：按主背包物品顺序建立轮盘映射
- **间隔处理**：支持物品间隔的智能插入
- **位置移动**：插入新物品时，后续物品自动后移
- **容量限制**：最多8个轮盘位置

### 3. 位置同步功能
- **双向映射**：轮盘位置 ↔ 背包位置
- **位置调整**：轮盘中的位置变化同步到背包
- **实时更新**：背包变化自动刷新轮盘映射

## 🏗️ 架构设计

### 核心数据结构
```csharp
// 主背包引用
private Inventory _mainBackpackInventory;

// 轮盘位置到背包位置的映射（8个轮盘位置）
private readonly int[] _wheelToBackpackPositions = new int[8];

// 背包位置到轮盘位置的映射（反向查找）
private readonly Dictionary<int, int> _backpackToWheelPositions = new Dictionary<int, int>();

// 事件订阅标识
private bool _isSubscribedToBackpack = false;

// 当前角色引用
private CharacterMainControl _mainCharacter;
```

### 映射关系示例
```
轮盘位置 [0, 1, 2, 3, 4, 5, 6, 7]
    ↓ 映射
背包位置 [0, 1, 3, 4, 7, 9, 12, 15] (可能有间隔)
```

## 🔧 核心算法

### 1. 初始映射建立算法
```csharp
private void InitializeWheelMapping()
{
    // 清空映射
    Array.Fill(_wheelToBackpackPositions, -1);
    _backpackToWheelPositions.Clear();

    int wheelPosition = 0;

    // 遍历主背包，按顺序建立映射
    for (int backpackPosition = 0; backpackPosition < _mainBackpackInventory.Content.Count; backpackPosition++)
    {
        if (wheelPosition >= 8) break; // 轮盘只有8个位置

        var item = _mainBackpackInventory.GetItemAt(backpackPosition);
        if (item != null && IsShortcutItem(item))
        {
            // 建立映射关系
            _wheelToBackpackPositions[wheelPosition] = backpackPosition;
            _backpackToWheelPositions[backpackPosition] = wheelPosition;

            // 添加到轮盘显示
            var category = ItemCategorizer.CategorizeItem(item);
            if (category != ItemCategory.None)
            {
                _wheelLayoutManager.AddItemToCategory(category, item);
            }

            wheelPosition++;
        }
    }
}
```

### 2. 智能插入算法
```csharp
private void InsertItemAt(int backpackPosition, Item item)
{
    // 第1步：找到新物品应该插入的轮盘位置
    int insertWheelPos = FindInsertWheelPosition(backpackPosition);

    if (insertWheelPos == -1) return; // 轮盘已满

    // 第2步：从插入位置开始，将后面的轮盘物品向后移动
    ShiftWheelItemsBackward(insertWheelPos);

    // 第3步：在插入位置放入新物品
    _wheelToBackpackPositions[insertWheelPos] = backpackPosition;
    _backpackToWheelPositions[backpackPosition] = insertWheelPos;

    // 第4步：更新轮盘UI
    var category = ItemCategorizer.CategorizeItem(item);
    if (category != ItemCategory.None)
    {
        _wheelLayoutManager.AddItemToCategory(category, item);
    }
}
```

### 3. 插入位置查找算法
```csharp
private int FindInsertWheelPosition(int backpackPosition)
{
    for (int wheelPos = 0; wheelPos < 8; wheelPos++)
    {
        int existingBackpackPos = _wheelToBackpackPositions[wheelPos];

        // 情况1：轮盘位置为空，可以插入
        if (existingBackpackPos == -1) return wheelPos;

        // 情况2：现有物品的背包位置大于新物品位置，应该插入在此位置之前
        if (existingBackpackPos > backpackPosition) return wheelPos;
    }

    return -1; // 轮盘已满
}
```

### 4. 向后移动算法
```csharp
private void ShiftWheelItemsBackward(int fromWheelPosition)
{
    // 从后向前，将轮盘物品向后移动一位
    for (int wheelPos = 7; wheelPos > fromWheelPosition; wheelPos--)
    {
        int currentBackpackPos = _wheelToBackpackPositions[wheelPos - 1];

        if (currentBackpackPos != -1)
        {
            // 将物品从轮盘位置 wheelPos-1 移动到 wheelPos
            _wheelToBackpackPositions[wheelPos] = currentBackpackPos;
            _backpackToWheelPositions[currentBackpackPos] = wheelPos;

            // 更新UI：移动对应的轮盘物品
            var item = _mainBackpackInventory.GetItemAt(currentBackpackPos);
            if (item != null)
            {
                var category = ItemCategorizer.CategorizeItem(item);
                if (category != ItemCategory.None)
                {
                    _wheelLayoutManager.AddItemToCategory(category, item);
                }
            }
        }
    }

    // 清空原始插入位置（将被新物品占用）
    _wheelToBackpackPositions[fromWheelPosition] = -1;
}
```

### 5. 位置同步算法
```csharp
public void AdjustWheelPosition(int fromWheelPos, int toWheelPos)
{
    int fromBackpackPos = _wheelToBackpackPositions[fromWheelPos];
    int toBackpackPos = _wheelToBackpackPositions[toWheelPos];

    if (fromBackpackPos == -1) return; // 源位置为空

    var item = _mainBackpackInventory.GetItemAt(fromBackpackPos);
    if (item == null) return;

    try
    {
        // 交换背包中的物品位置
        if (toBackpackPos != -1)
        {
            // 目标位置有物品，交换
            var targetItem = _mainBackpackInventory.GetItemAt(toBackpackPos);
            if (targetItem != null)
            {
                _mainBackpackInventory.AddAt(targetItem, fromBackpackPos);
            }
        }

        _mainBackpackInventory.AddAt(item, toBackpackPos);

        // 更新映射关系
        _wheelToBackpackPositions[fromWheelPos] = toBackpackPos;
        _wheelToBackpackPositions[toWheelPos] = fromBackpackPos;

        if (toBackpackPos != -1)
        {
            _backpackToWheelPositions[toBackpackPos] = fromWheelPos;
        }
        _backpackToWheelPositions[fromBackpackPos] = toWheelPos;
    }
    catch (Exception ex)
    {
        Plugin.LogError($"MainBackpackWheelManager: 位置同步失败: {ex.Message}");
    }
}
```

## 📊 事件处理机制

### 事件订阅
```csharp
private void SubscribeToBackpackEvents()
{
    if (_isSubscribedToBackpack || _mainBackpackInventory == null) return;

    // 订阅主背包内容变化事件
    _mainBackpackInventory.onContentChanged += OnMainBackpackContentChanged;
    _isSubscribedToBackpack = true;
}
```

### 事件处理
```csharp
private void OnMainBackpackContentChanged()
{
    if (_isSubscribedToBackpack)
    {
        Plugin.Log("MainBackpackWheelManager: 主背包内容发生变化，刷新轮盘映射");
        RefreshWheelMapping();
    }
}
```

## 🎮 用户体验特性

### 1. 直观的位置对应
- 轮盘位置与背包位置保持直观的对应关系
- 用户可以通过轮盘位置快速定位背包中的物品

### 2. 智能的插入逻辑
- 新物品插入时，保持轮盘与背包的顺序一致性
- 支持物品间隔的智能处理

### 3. 实时同步更新
- 背包变化自动反映到轮盘
- 轮盘中的操作同步到背包

### 4. 容量管理
- 最多支持8个轮盘位置
- 超出容量时提供清晰的反馈

## 🔄 与配件系统模式的区别

| 特性 | 配件系统模式 | 主背包模式 |
|------|------------|----------|
| 数据源 | 主背包 + 配件物品 | 仅主背包物品 |
| 启用条件 | 装备支持的背包 | 任何背包都可用 |
| 快捷键 | 自动更新到4/5/Q/G | 保持原版设置不变 |
| 轮盘容量 | 每个类别最多8个 | 总共最多8个 |
| 位置管理 | 配件系统管理 | 背包位置映射 |

## 🔧 扩展接口

### 公共方法
```csharp
// 获取轮盘位置对应的背包位置
public int GetBackpackPosition(int wheelPosition)

// 获取背包位置对应的轮盘位置
public int GetWheelPosition(int backpackPosition)

// 调整轮盘物品位置（同步到背包）
public void AdjustWheelPosition(int fromWheelPos, int toWheelPos)
```

## 📋 使用示例

### 基本使用
```csharp
// 创建管理器
var mainBackpackManager = new MainBackpackWheelManager(wheelLayoutManager);

// 初始化
mainBackpackManager.Initialize();

// 获取轮盘位置对应的背包位置
int backpackPos = mainBackpackManager.GetBackpackPosition(0);

// 调整轮盘位置（同步到背包）
mainBackpackManager.AdjustWheelPosition(0, 1);
```

### 模式切换
```csharp
// 切换到主背包模式
backpackShortcutManager.SwitchToMainBackpackMode();

// 切换回配件系统模式
backpackShortcutManager.SwitchToAttachmentMode();
```

## ⚠️ 注意事项

1. **依赖关系**：需要主角色和主背包的有效引用
2. **事件管理**：正确的事件订阅和取消订阅
3. **异常处理**：背包操作可能失败，需要异常处理
4. **性能考虑**：避免频繁的背包操作，使用事件驱动更新

## 🚀 后续优化方向

1. **性能优化**：减少不必要的刷新操作
2. **用户体验**：添加更多的视觉反馈
3. **配置选项**：允许用户自定义轮盘容量和过滤规则
4. **批量操作**：支持批量位置调整