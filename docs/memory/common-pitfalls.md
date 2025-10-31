# 常见陷阱和错误

记录项目中常见的错误、陷阱和解决方案，避免重复踩坑。

## 🚨 Item类相关陷阱

### 陷阱1: 使用Length访问Slots
**错误代码**:
```csharp
int count = item.Slots.Length;  // ❌ 编译错误！
```

**根本原因**: Item.Slots是SlotCollection类型，不是数组，没有Length属性

**正确做法**:
```csharp
int count = item.Slots.Count;   // ✅ 使用Count属性
```

**预防措施**:
- 记住SlotCollection的特点
- 使用IDE的智能提示避免错误
- 编写单元测试验证基础操作

### 陷阱2: 不做null检查导致异常
**错误代码**:
```csharp
GameObject container = _field.GetValue(instance) as GameObject;
container.SetActive(true);  // ❌ 可能NullReferenceException
```

**根本原因**: 反射获取的对象可能为null，直接使用会抛异常

**正确做法**:
```csharp
if (_field == null) return;
GameObject container = _field.GetValue(instance) as GameObject;
if (container == null) return;
container.SetActive(true);  // ✅ 安全操作
```

**预防措施**:
- 所有反射操作都必须null检查
- 使用guard clause模式提前返回
- 添加防御性编程检查

### 陷阱3: 异步删除导致的后续操作失败
**错误代码**:
```csharp
Object.Destroy(oldLayout);           // ❌ 异步删除
var newLayout = container.AddComponent<GridLayoutGroup>(); // 可能失败
```

**根本原因**: Destroy是异步操作，对象可能还没完全删除就尝试添加新组件

**正确做法**:
```csharp
Object.DestroyImmediate(oldLayout);  // ✅ 同步删除
var newLayout = container.AddComponent<GridLayoutGroup>();
if (newLayout == null) return;       // 防守检查
```

**预防措施**:
- 需要立即生效时使用DestroyImmediate
- 添加组件后检查是否成功
- 使用try-catch包裹关键操作

## ⚡ 参数理解陷阱

### 陷阱1: 气泡速度参数误解
**错误认知**: speed数值越小越快
**错误使用**:
```csharp
playerCharacter.PopText(text, 0.1f);  // ❌ 实际是超级慢！
```

**源码真相**: `DialogueBubble.cs:137` - `defaultSpeed = 10f`，speed越大越快

**正确做法**:
```csharp
playerCharacter.PopText(text, 50f);   // ✅ 5倍默认速度
```

**预防措施**:
- 永远不要基于参数名称猜测功能
- 查看源码确认参数真实含义
- 进行小范围测试验证效果

### 陷阱2: UI更新时机错误
**错误做法**:
```csharp
item.Slots[0].Content = newItem;  // 修改数据
// ❌ 忘记更新UI，用户看不到变化
```

**正确做法**:
```csharp
item.Slots[0].Content = newItem;  // 修改数据
UpdateUI();                       // ✅ 立即更新UI显示
```

**预防措施**:
- 数据变化后立即更新UI
- 使用事件驱动机制自动更新
- 测试时确认UI是否同步更新

## 🔄 异步和协程陷阱

### 陷阱1: 协程参数传递错误
**错误代码**:
```csharp
StartCoroutine(MyCoroutine(out result));  // ❌ 协程不能使用out参数
```

**根本原因**: 协程是异步执行，不能使用out/ref参数

**正确做法**:
```csharp
// 使用封装的数据结构
class CoroutineResult { public bool success; }
var result = new CoroutineResult();
StartCoroutine(MyCoroutine(result));
```

**预防措施**:
- 避免在协程中使用out/ref参数
- 使用封装的数据结构传递结果
- 理解协程的异步执行特性

### 陷阱2: 递归调用无限循环
**错误代码**:
```csharp
void ProcessItem(Item item)
{
    // ❌ 没有递归检测，可能导致无限循环
    foreach (var slot in item.Slots)
    {
        if (slot.Content != null)
            ProcessItem(slot.Content); // 可能循环引用
    }
}
```

**正确做法**:
```csharp
void ProcessItem(Item item, HashSet<Item> visited = null)
{
    visited = visited ?? new HashSet<Item>();
    if (visited.Contains(item)) return; // ✅ 递归检测

    visited.Add(item);
    foreach (var slot in item.Slots)
    {
        if (slot.Content != null)
            ProcessItem(slot.Content, visited);
    }
}
```

**预防措施**:
- 添加递归检测机制
- 使用HashSet跟踪已访问对象
- 设置最大递归深度限制

## 🎯 性能陷阱

### 陷阱1: 频繁的字符串操作
**错误代码**:
```csharp
string result = "";
for (int i = 0; i < 1000; i++)
{
    result += $"Item {i}\n";  // ❌ 每次都创建新字符串
}
```

**正确做法**:
```csharp
StringBuilder sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.AppendLine($"Item {i}");  // ✅ 高效的字符串构建
}
string result = sb.ToString();
```

**预防措施**:
- 大量字符串拼接使用StringBuilder
- 避免在循环中创建临时对象
- 使用对象池复用频繁创建的对象

### 陷阱2: 同步处理大量数据
**错误代码**:
```csharp
// ❌ 在同一帧处理大量物品，导致卡顿
foreach (var item in allItems)
{
    ProcessItem(item); // 可能很耗时
}
```

**正确做法**:
```csharp
// ✅ 分帧处理，避免卡顿
StartCoroutine(ProcessItemsFrameDistributed(allItems));
```

**预防措施**:
- 大量数据处理使用分帧机制
- 监控每帧的处理时间
- 在性能敏感的代码中添加性能计时

## 🧪 调试陷阱

### 陷阱1: 过度日志输出
**错误做法**:
```csharp
// ❌ 在UI刷新时频繁打印日志
Debug.Log($"Processing item: {item.DisplayName}");
```

**问题**: 在UI频繁更新时产生大量日志，影响性能

**正确做法**:
```csharp
#if DEBUG
Debug.Log($"Processing item: {item.DisplayName}");  // 只在Debug模式输出
#endif
```

**预防措施**:
- 使用条件编译控制日志输出
- 在性能敏感区域减少日志
- 使用日志级别控制输出量

### 陷阱2: 断点调试异步代码
**问题**: 在协程或异步方法中设置断点可能导致时序问题

**正确做法**:
- 使用日志输出替代断点
- 理解异步执行时序
- 在关键节点添加状态检查

## 📋 陷阱预防检查清单

### 编码阶段检查
- [ ] Item.Slots是否使用Count而不是Length
- [ ] 所有反射操作是否有null检查
- [ ] Unity对象删除是否使用DestroyImmediate
- [ ] 协程参数是否避免out/ref
- [ ] 是否添加递归检测

### 测试阶段检查
- [ ] 边界情况是否测试（null值、空集合等）
- [ ] 性能敏感操作是否有分帧处理
- [ ] UI更新是否与数据变化同步
- [ ] 异步操作时序是否正确

### 代码审查检查
- [ ] 是否有基于猜测的实现
- [ ] 是否查阅了相关源码
- [ ] 是否使用了官方API
- [ ] 是否有完整的错误处理

---
**更新时间**: 2025-10-31
**核心原则**: 防御性编程，源码验证，充分测试