# 委托（Delegate）模式与系统解耦

## 🎯 委托的基本概念

### 什么是委托？
- 委托是C#中的一个**类型**，就像"函数指针"的容器
- 委托可以**存放一个或多个方法**，通过调用委托来执行这些方法
- 委托是类型安全的，编译时会检查方法签名是否匹配

## 🔍 委托在Backpack_QuickWheel项目中的具体应用

### 1. 解决循环依赖问题
**问题场景**：ShortcutUIUpdater需要知道WheelLayoutManager的当前选择，但不应该直接依赖该类

**传统方式的问题**：
```csharp
// ❌ 直接依赖产生循环
public static class ShortcutUIUpdater
{
    private WheelLayoutManager _wheelLayoutManager; // 直接依赖
}
```

**委托解决方案**：
```csharp
// ✅ 使用委托作为"桥梁"
public static class ShortcutUIUpdater
{
    // 委托定义 - 像一个"功能插槽"
    public static Func<ItemCategory, Item> GetCurrentSelectionDelegate { get; set; }
}
```

### 2. 委托语法详解
```csharp
// Func是C#内置的泛型委托
Func<ItemCategory, Item>
//  ↑           ↑        ↑
// 返回类型    参数类型  参数名

// 含义：接受ItemCategory参数，返回Item的方法签名
```

### 3. 实际使用流程
```csharp
// 第1步：设置委托（BackpackShortcutManager在初始化时）
ShortcutUIUpdater.GetCurrentSelectionDelegate = _wheelLayoutManager.GetCurrentSelection;

// 第2步：使用委托（ShortcutUIUpdater在需要时调用）
var currentItem = GetCurrentSelectionDelegate?.Invoke(category);
//                        ↑ 实际调用_wheelLayoutManager.GetCurrentSelection(category)
```

## 🏗️ 委托模式的优势

### 解耦设计
- **没有委托时**：类之间直接引用，产生循环依赖，难以测试和维护
- **使用委托后**：类之间通过函数签名合作，避免直接依赖

### 职责分离
- **ShortcutUIUpdater**：定义功能需求 "我需要知道当前选了什么来更新UI"
- **BackpackShortcutManager**：提供功能实现 "我告诉你当前选了什么"
- **委托**：连接需求和实现的桥梁

### 灵活性
- **运行时绑定**：可以在运行时改变委托指向的方法
- **支持多播**：一个委托可以指向多个方法
- **易于测试**：可以为委托设置mock对象进行单元测试

## 🎯 类比理解

### 遥控器类比
- **委托**：遥控器
- **ShortcutUIUpdater**：拿遥控器的人，知道按哪个按钮，但不知道控制哪个电视
- **BackpackShortcutManager**：把电视控制方法"编程"到遥控器里
- **调用时**：按遥控器按钮，电视响应

### 接口类比（更准确）
- **委托定义**：像接口定义 "我需要一个能告诉我当前选择什么的功能"
- **委托实现**：像接口实现 "这个功能我来提供"
- **更灵活**：委托可以在运行时动态改变实现，接口在编译时固定

## ✅ 关键技术要点

### 1. 委托是类型
```csharp
// 可以声明变量
Func<ItemCategory, Item> myDelegate;

// 可以作为参数
public void ProcessCategory(Func<ItemCategory, Item> selector) { }

// 可以作为返回值
public Func<ItemCategory, Item> CreateSelector() { return _wheelLayoutManager.GetCurrentSelection; }
```

### 2. 函数签名匹配
委托和方法必须有相同的参数和返回类型：
```csharp
// 委托签名
Func<ItemCategory, Item> selector;

// 匹配的方法签名
public Item GetCurrentSelection(ItemCategory category) { /* ... */ }

// 不匹配的方法签名（参数类型不同）
public Item GetSelectionByName(string name) { /* ... */ } // ❌ 不匹配
```

### 3. 空值检查
```csharp
// 使用 ?. 操作符安全调用委托
var result = GetCurrentSelectionDelegate?.Invoke(category);

// 等价于：
if (GetCurrentSelectionDelegate != null)
{
    var result = GetCurrentSelectionDelegate.Invoke(category);
}
```

### 4. 多播委托
```csharp
// 一个委托可以指向多个方法
Func<ItemCategory, Item> multiDelegate = _wheelLayoutManager.GetCurrentSelection;
multiDelegate += _fallbackManager.GetCurrentSelection;

// 调用时所有方法都会执行
```

## 🔧 实际应用场景

### 在Backpack_QuickWheel项目中的具体应用：

1. **UI系统自我管理**：
   - ShortcutUIUpdater需要知道当前选择来更新UI
   - 使用委托避免直接依赖WheelLayoutManager
   - 实现UI系统的独立性和可测试性

2. **系统间通信**：
   - 不同系统之间通过委托进行松耦合通信
   - 避免类之间的直接引用
   - 支持运行时动态配置

3. **测试友好性**：
   - 可以轻松为委托设置mock对象
   - 便于单元测试和集成测试
   - 提高代码的可维护性

## 🚀 总结

委托是C#中实现松耦合设计的重要工具。在Backpack_QuickWheel项目中，我们使用委托：

1. **解决循环依赖**：UI系统不需要直接依赖数据系统
2. **实现职责分离**：每个系统专注自己的核心功能
3. **提高系统灵活性**：支持运行时配置和动态替换
4. **增强可测试性**：便于单元测试和模块化开发

这种设计模式让整个系统更加模块化、可维护和可扩展。

---
*记录时间：游戏重启修复和架构重构项目*
*应用场景：ShortcutUIUpdater自我管理功能实现*