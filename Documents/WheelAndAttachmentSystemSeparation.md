# 轮盘与配件系统分离设计文档

## 📋 项目概述

### 目标
实现轮盘系统与配件系统的分离，让玩家可以选择是否启用背包配件影响快捷键，支持两种独立的运行模式：
- **配件系统模式**：当前的完整功能（主背包 + 配件 + 快捷键自动更新）
- **主背包模式**：纯轮盘功能，数据源为主背包，快捷键保持原版设置

### 核心要求
1. **数据源切换**：关闭配件系统时，数据源切换为主背包物品
2. **位置一致性**：轮盘与主背包物品位置保持同步
3. **快速切换**：通过F9按键快速切换模式
4. **架构清晰**：两种模式状态隔离，互不干扰

## 🏗️ 架构设计

### 现有架构分析
```
数据流: BackpackShortcutManager -> WheelLayoutManager -> 轮盘显示
数据源: 配件系统 -> 三层事件订阅 -> 物品收集 -> 轮盘更新
```

### 新架构设计
```
BackpackShortcutManager (模式切换器)
├── IWheelDataManager (接口抽象)
├── AttachmentWheelManager (配件系统管理器)
└── MainBackpackWheelManager (主背包管理器)
    └── 共享: WheelLayoutManager (显示层)
```

## 🔧 核心组件设计

### 1. IWheelDataManager 接口
```csharp
public interface IWheelDataManager
{
    void Initialize();           // 初始化
    void Shutdown();             // 清理资源
    void HandleBackpackChange(Item backpack);  // 处理背包变化
    void OnGameStart();          // 游戏开始处理
}
```

### 2. 配件系统模式 (AttachmentWheelManager)
**特点**：
- 保持现有配件系统逻辑不变
- 支持主背包 + 配件物品的混合数据源
- 快捷键自动更新到4/5/Q/G
- 只有装备支持背包时才启用

**核心逻辑**：
```csharp
public class AttachmentWheelManager : IWheelDataManager
{
    // 现有配件系统逻辑的封装
    private Dictionary<Item, HashSet<ItemCategory>> _attachmentCategories;
    private Dictionary<Item, Item> _parentAttachmentMap;

    public void HandleBackpackChange(Item backpack)
    {
        if (IsBackpackSupported(backpack))
        {
            // 启用配件系统：主背包 + 配件
            CollectFromAttachmentSystem(backpack);
        }
        else
        {
            // 禁用系统
            DisableSystem();
        }
    }
}
```

### 3. 主背包模式 (MainBackpackWheelManager)
**特点**：
- 数据源为主背包Inventory
- 轮盘总共8个位置，按主背包物品顺序填充
- 保持轮盘与背包位置的双向同步
- 快捷键保持原版设置不变

**核心设计**：
```csharp
public class MainBackpackWheelManager : IWheelDataManager
{
    // 轮盘位置到背包位置的映射（8个轮盘位置）
    private readonly int[] _wheelToBackpackPositions = new int[8];

    // 背包位置到轮盘位置的映射（反向查找）
    private readonly Dictionary<int, int> _backpackToWheelPositions = new Dictionary<int, int>();

    private Inventory _mainBackpackInventory;

    public void Initialize()
    {
        // 获取主背包引用并订阅事件
        var mainCharacter = CharacterMainControl.Main;
        _mainBackpackInventory = mainCharacter.CharacterItem.Inventory;
        _mainBackpackInventory.onContentChanged += OnMainBackpackContentChanged;

        // 建立初始映射关系
        InitializeWheelMapping();
    }
}
```

## 🎯 关键算法设计

### 1. 初始映射建立算法
```csharp
private void InitializeWheelMapping()
{
    // 清空映射
    Array.Fill(_wheelToBackpackPositions, -1);
    _backpackToWheelPositions.Clear();

    int wheelPosition = 0;

    for (int backpackPosition = 0; backpackPosition < _mainBackpackInventory.Content.Count; backpackPosition++)
    {
        if (wheelPosition >= 8) break; // 轮盘只有8个位置

        var item = _mainBackpackInventory.GetItemAt(backpackPosition);
        if (item != null && ItemCategorizer.IsShortcutItem(item))
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

### 2. 智能插入算法（核心）
**场景**：主背包物品有间隔，新物品插入到间隔位置时，轮盘需要按顺序调整

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
    _wheelLayoutManager.AddItemToCategory(category, item);
}

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
                _wheelLayoutManager.AddItemToCategory(category, item);
            }
        }
    }

    // 清空原始插入位置（将被新物品占用）
    _wheelToBackpackPositions[fromWheelPosition] = -1;
}
```

### 3. 位置同步算法
```csharp
// 在轮盘中调整物品位置时，同步调整背包中的位置
public void AdjustWheelPosition(int fromWheelPos, int toWheelPos)
{
    if (fromWheelPos < 0 || fromWheelPos >= 8 || toWheelPos < 0 || toWheelPos >= 8) return;

    int fromBackpackPos = _wheelToBackpackPositions[fromWheelPos];
    int toBackpackPos = _wheelToBackpackPositions[toWheelPos];

    if (fromBackpackPos == -1) return; // 源位置为空

    var item = _mainBackpackInventory.GetItemAt(fromBackpackPos);
    if (item == null) return;

    // 交换背包中的物品位置
    if (toBackpackPos != -1)
    {
        // 目标位置有物品，交换
        var targetItem = _mainBackpackInventory.GetItemAt(toBackpackPos);
        _mainBackpackInventory.AddAt(targetItem, fromBackpackPos);
        _mainBackpackInventory.AddAt(item, toBackpackPos);
    }
    else
    {
        // 目标位置为空，移动
        _mainBackpackInventory.RemoveAt(fromBackpackPos, out _);
        _mainBackpackInventory.AddAt(item, toBackpackPos);
    }
}
```

## 🔄 模式切换机制

### 切换触发器
```csharp
// 在InputInterceptor中处理F9按键
private void Update()
{
    // 现有轮盘逻辑...

    // 检测F9切换键
    if (Keyboard.current.f9Key.wasPressedThisFrame)
    {
        // 防止在轮盘显示过程中切换
        if (!_wheelLayoutManager.IsWheelVisible)
        {
            _backpackShortcutManager.ToggleSystemMode();
        }
    }
}
```

### 模式切换逻辑
```csharp
public class BackpackShortcutManager
{
    private bool _isAttachmentMode = true;
    private AttachmentWheelManager _attachmentManager;
    private MainBackpackWheelManager _mainBackpackManager;
    private IWheelDataManager _activeManager;

    public void ToggleSystemMode()
    {
        // 1. 切换配置
        _isAttachmentMode = !_isAttachmentMode;
        BackpackModConfig.EnableAttachmentSystem = _isAttachmentMode;
        BackpackModConfig.SaveConfig();

        // 2. 清空当前轮盘
        _wheelLayoutManager.ClearAllCategories();

        // 3. 停用当前管理器
        _activeManager?.Shutdown();

        // 4. 切换到新管理器
        _activeManager = _isAttachmentMode ? _attachmentManager : _mainBackpackManager;

        // 5. 启用新管理器
        _activeManager.Initialize();

        // 6. 显示切换提示
        string modeName = _isAttachmentMode ? "配件系统模式" : "主背包模式";
        NotificationManager.ShowMessage($"已切换到: {modeName} (F9切换)", 3.0f);
    }
}
```

## 📊 数据结构设计

### 配件系统模式数据结构
```csharp
public class AttachmentWheelManager
{
    // 配件类别跟踪
    private Dictionary<Item, HashSet<ItemCategory>> _attachmentCategories;

    // 配件到父物品的映射
    private Dictionary<Item, Item> _parentAttachmentMap;

    // 当前装备的配件集合
    private HashSet<Item> _currentAttachments;
}
```

### 主背包模式数据结构
```csharp
public class MainBackpackWheelManager
{
    // 轮盘位置到背包位置的映射（8个轮盘位置）
    private readonly int[] _wheelToBackpackPositions = new int[8];

    // 背包位置到轮盘位置的映射（反向查找）
    private readonly Dictionary<int, int> _backpackToWheelPositions = new Dictionary<int, int>();

    // 主背包引用
    private Inventory _mainBackpackInventory;

    // 事件订阅标识
    private bool _isSubscribedToBackpack = false;
}
```

## 🎮 用户体验设计

### 模式行为对比

| 特性 | 配件系统模式 | 主背包模式 |
|------|------------|----------|
| 数据源 | 主背包 + 配件物品 | 仅主背包物品 |
| 启用条件 | 装备支持的背包 | 任何背包都可用 |
| 快捷键 | 自动更新到4/5/Q/G | 保持原版设置不变 |
| 轮盘容量 | 每个类别最多8个 | 总共最多8个 |
| 位置管理 | 配件系统管理 | 背包位置映射 |

### 用户交互流程
1. **模式切换**：按F9键切换模式，显示提示信息
2. **位置调整**：在轮盘中调整位置会同步更新背包
3. **自动补充**：轮盘中物品被使用后，自动从背包补充
4. **配置持久化**：模式选择自动保存，下次游戏保持

## 🔧 配置管理

### 配置项
```csharp
public static class BackpackModConfig
{
    // 配件系统开关
    public static bool EnableAttachmentSystem = true;

    // 配置持久化
    public static void LoadConfig()
    {
        EnableAttachmentSystem = OptionsManager.Load("Backpack_EnableAttachmentSystem", true);
    }

    public static void SaveConfig()
    {
        OptionsManager.Save("Backpack_EnableAttachmentSystem", EnableAttachmentSystem);
    }
}
```

## 📋 实施计划

### 阶段1：基础架构（1-2天）
- [ ] 创建IWheelDataManager接口抽象
- [ ] 修改BackpackShortcutManager支持模式切换
- [ ] 实现配置持久化

### 阶段2：封装配件系统（1天）
- [ ] 创建AttachmentWheelManager
- [ ] 将现有配件系统逻辑迁移到新管理器
- [ ] 确保配件系统功能正常

### 阶段3：实现主背包系统（2-3天）
- [ ] 实现MainBackpackWheelManager
- [ ] 实现位置映射算法
- [ ] 实现智能插入算法
- [ ] 实现位置同步功能

### 阶段4：集成和测试（1-2天）
- [ ] 实现F9切换功能
- [ ] 模式切换测试
- [ ] 边界情况处理
- [ ] 性能优化

## ⚠️ 关键约束和注意事项

### 约束条件
1. **轮盘容量限制**：总共8个位置，不是每个类别8个
2. **Tag过滤规则**：只收集Healing、Injector、Food、Explosive、MeleeWeapon标签的物品
3. **位置映射维护**：确保轮盘与背包位置的双向同步
4. **事件订阅优化**：避免订阅过多事件影响性能

### 注意事项
1. **状态隔离**：两种模式的状态完全独立，切换时需要清理
2. **事件处理**：主背包物品变化频繁，需要优化事件处理逻辑
3. **用户反馈**：模式切换时提供清晰的提示信息
4. **向后兼容**：确保现有配件系统功能不受影响

## 🧪 测试用例

### 基础功能测试
- [ ] 配件系统模式正常工作
- [ ] 主背包模式正常工作
- [ ] F9切换功能正常
- [ ] 配置保存和加载正常

### 边界情况测试
- [ ] 轮盘已满时新物品处理
- [ ] 物品间隔插入算法验证
- [ ] 背包容量变化时的处理
- [ ] 快速连续切换模式的处理

### 性能测试
- [ ] 主背包物品频繁变化的性能
- [ ] 大量物品时的插入算法性能
- [ ] 内存使用情况监控

## 📝 开发记录

### 关键决策记录
1. **数据源选择**：采用主背包Inventory作为数据源，通过onContentChanged事件监听变化
2. **位置映射策略**：使用数组+字典的双向映射，支持快速查找
3. **插入算法**：采用向后移动策略，保持轮盘与背包的顺序一致性
4. **架构设计**：通过接口抽象实现两种模式的隔离和切换

### 技术难点和解决方案
1. **物品间隔处理**：通过FindInsertWheelPosition和ShiftWheelItemsBackward算法解决
2. **状态隔离**：通过独立的管理器类实现状态隔离
3. **事件订阅冲突**：通过Shutdown方法确保事件订阅的正确切换
4. **位置同步**：通过双向映射和Inventory操作实现位置同步

---

**文档版本**: 1.0
**创建日期**: 2024-11-04
**最后更新**: 2024-11-04
**作者**: Claude Code Assistant