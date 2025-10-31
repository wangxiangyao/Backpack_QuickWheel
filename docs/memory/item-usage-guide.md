# Item类使用完整指南

## ⚠️ 核心规则

**Item.Slots 是 SlotCollection 类型，不是数组！**
必须使用 `.Count` 属性，永远不能使用 `.Length`

## 🔍 Item类核心属性

### 基础属性
```csharp
item.DisplayName        // 物品显示名称
item.ItemID            // 物品唯一标识
item.Weight            // 物品重量
item.Value             // 物品价值
item.Stackable         // 是否可堆叠
item.StackCount        // 当前堆叠数量
item.MaxStack          // 最大堆叠数量
```

### 插槽相关（重要！）
```csharp
// ✅ 正确用法
SlotCollection slots = item.Slots;        // 获取插槽集合
int slotCount = slots.Count;              // 获取插槽数量
Slot firstSlot = slots[0];                // 访问第一个插槽
Item slotContent = slots[0].Content;      // 获取插槽内容

// ❌ 致命错误
int wrongCount = item.Slots.Length;       // 编译错误！SlotCollection没有Length
```

### 状态检查
```csharp
item.IsBeingDestroyed    // 物品是否正在被销毁
item.ParentItem         // 父容器物品
item.Owner              // 拥有者
```

## 🎯 常见使用模式

### 1. 遍历所有插槽
```csharp
// ✅ 正确遍历
foreach (Slot slot in item.Slots)
{
    if (slot != null && slot.Content != null)
    {
        Item content = slot.Content;
        Debug.Log($"插槽内容: {content.DisplayName}");
    }
}
```

### 2. 检查插槽是否为空
```csharp
// ✅ 安全检查
Slot slot = item.Slots[index];
if (slot != null && slot.Content == null)
{
    // 插槽为空，可以放置物品
}
```

### 3. 放置物品到插槽
```csharp
// ✅ 安全放置
if (slot != null && slot.Content == null)
{
    slot.Content = itemToPlace;
    // 触发UI更新
}
```

### 4. 物品有效性检查
```csharp
// ✅ 完整有效性检查
bool IsValidItem(Item item)
{
    return item != null &&
           !item.IsBeingDestroyed &&
           item.ParentItem != null;
}
```

## 🚨 常见错误和修复

### 错误1: 使用Length访问Slots
```csharp
// ❌ 错误
int count = item.Slots.Length;

// ✅ 修复
int count = item.Slots.Count;
```

### 错误2: 不做null检查
```csharp
// ❌ 错误
Item content = item.Slots[0].Content;
Debug.Log(content.DisplayName); // 可能NullReferenceException

// ✅ 修复
Slot slot = item.Slots[0];
if (slot != null && slot.Content != null)
{
    Item content = slot.Content;
    Debug.Log(content.DisplayName);
}
```

### 错误3: 不检查物品状态
```csharp
// ❌ 错误
foreach (Item item in allItems)
{
    ProcessItem(item); // 可能处理已销毁的物品
}

// ✅ 修复
foreach (Item item in allItems)
{
    if (item != null && !item.IsBeingDestroyed)
    {
        ProcessItem(item);
    }
}
```

## 🔧 高级用法

### 1. 递归遍历嵌套插槽
```csharp
void ExploreNestedItems(Item item, int depth = 0)
{
    if (item == null || item.IsBeingDestroyed) return;

    string indent = new string(' ', depth * 2);
    Debug.Log($"{indent}- {item.DisplayName}");

    foreach (Slot slot in item.Slots)
    {
        if (slot?.Content != null)
        {
            ExploreNestedItems(slot.Content, depth + 1);
        }
    }
}
```

### 2. 插槽容量计算
```csharp
int CalculateTotalCapacity(Item container)
{
    int total = 0;
    foreach (Slot slot in container.Slots)
    {
        if (slot != null) total++;
    }
    return total;
}

int CalculateUsedCapacity(Item container)
{
    int used = 0;
    foreach (Slot slot in container.Slots)
    {
        if (slot?.Content != null) used++;
    }
    return used;
}
```

### 3. 物品查找
```csharp
Item FindItemByName(Item container, string targetName)
{
    if (container.DisplayName == targetName)
        return container;

    foreach (Slot slot in container.Slots)
    {
        if (slot?.Content != null)
        {
            Item found = FindItemByName(slot.Content, targetName);
            if (found != null) return found;
        }
    }
    return null;
}
```

## 📋 性能优化建议

### 1. 缓存插槽数量
```csharp
// ✅ 缓存优化
int slotCount = item.Slots.Count; // 只获取一次
for (int i = 0; i < slotCount; i++)
{
    // 使用缓存的数量
}
```

### 2. 避免频繁的字符串操作
```csharp
// ❌ 性能差
for (int i = 0; i < item.Slots.Count; i++)
{
    Debug.Log($"Slot {i}: {item.Slots[i].Content?.DisplayName}");
}

// ✅ 性能好
StringBuilder sb = new StringBuilder();
for (int i = 0; i < item.Slots.Count; i++)
{
    sb.AppendLine($"Slot {i}: {item.Slots[i].Content?.DisplayName}");
}
Debug.Log(sb.ToString());
```

### 3. 使用对象池
```csharp
// 对于频繁创建的临时对象，考虑使用对象池
private static readonly List<Item> _tempItemList = new List<Item>();

void ProcessItemsEfficiently(Item container)
{
    _tempItemList.Clear();
    CollectItems(container, _tempItemList);

    // 处理物品...
}
```

---
**更新时间**: 2025-10-31
**核心要点**: 用Count不用Length，必须null检查，注意物品状态