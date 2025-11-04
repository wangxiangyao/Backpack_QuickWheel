# BackpackShortcutManager 模式切换实现说明

## 📋 概述

BackpackShortcutManager 现在支持两种独立的运行模式：
- **配件系统模式**：当前的完整功能（主背包 + 配件 + 快捷键自动更新）
- **主背包模式**：纯轮盘功能，数据源为主背包

## 🏗️ 架构设计

### 核心组件
```csharp
// 模式切换相关字段
private bool _isAttachmentMode = true;
private AttachmentWheelManager _attachmentManager;
private MainBackpackWheelManager _mainBackpackManager;
private IWheelDataManager _activeManager;
```

### 管理器关系
```
BackpackShortcutManager (模式切换器)
├── IWheelDataManager (接口抽象)
├── AttachmentWheelManager (配件系统管理器)
└── MainBackpackWheelManager (主背包管理器)
    └── 共享: WheelLayoutManager (显示层)
```

## 🔧 核心功能实现

### 1. 模式管理器初始化
```csharp
private void InitializeModeManagers()
{
    Debug.Log("[BackpackShortcutManager] 初始化模式管理器");

    // 创建两个管理器实例
    _attachmentManager = new AttachmentWheelManager(_wheelLayoutManager);
    _mainBackpackManager = new MainBackpackWheelManager(_wheelLayoutManager);

    Debug.Log("[BackpackShortcutManager] 模式管理器初始化完成");
}
```

### 2. 配置持久化
```csharp
private void LoadModeFromConfig()
{
    _isAttachmentMode = BackpackModConfig.EnableAttachmentSystem;
    Debug.Log($"[BackpackShortcutManager] 从配置加载模式: {(_isAttachmentMode ? "配件系统模式" : "主背包模式")}");

    // 设置初始活跃管理器
    _activeManager = _isAttachmentMode ? _attachmentManager : _mainBackpackManager;
}
```

### 3. 模式切换核心逻辑
```csharp
public void ToggleSystemMode()
{
    // 1. 切换配置
    _isAttachmentMode = !_isAttachmentMode;
    BackpackModConfig.EnableAttachmentSystem = _isAttachmentMode;
    BackpackModConfig.SaveConfig();

    // 2. 清空当前轮盘
    _wheelLayoutManager.ClearAllCategories();

    // 3. 停用当前管理器
    if (_activeManager != null)
    {
        _activeManager.Shutdown();
    }

    // 4. 切换到新管理器
    _activeManager = _isAttachmentMode ? _attachmentManager : _mainBackpackManager;

    // 5. 启用新管理器
    if (_activeManager != null)
    {
        _activeManager.Initialize();
    }

    // 6. 显示切换提示
    string modeName = _isAttachmentMode ? "配件系统模式" : "主背包模式";
    NotificationManager.ShowMessage($"已切换到: {modeName} (F9切换)", 3.0f);
}
```

### 4. 背包变化事件委托
```csharp
public void OnBackpackChanged(Slot backpackSlot)
{
    // 更新当前背包引用
    _currentBackpack = backpackSlot?.Content;

    // 清理当前状态
    UnsubscribeFromBackpackChanges();

    // 委托给活跃管理器处理
    if (_activeManager != null)
    {
        _activeManager.HandleBackpackChange(newBackpack);
    }

    // 对于配件系统模式，维护原有兼容性
    if (_isAttachmentMode && _activeManager == _attachmentManager)
    {
        // 保持原有的智能激活机制
        bool isSupportedBackpack = IsBackpackSupported(newBackpack);
        if (isSupportedBackpack)
        {
            HandleSupportedBackpack(newBackpack);
        }
        else
        {
            HandleUnsupportedBackpack(newBackpack);
        }
    }
}
```

## 🎯 公共接口

### 模式切换方法
```csharp
// 切换系统模式
public void ToggleSystemMode()

// 切换到配件系统模式
public void SwitchToAttachmentMode()

// 切换到主背包模式
public void SwitchToMainBackpackMode()
```

### 状态查询属性
```csharp
// 当前是否为配件系统模式
public bool IsAttachmentMode => _isAttachmentMode;

// 当前活跃的轮盘数据管理器
public IWheelDataManager ActiveManager => _activeManager;

// 轮盘布局管理器访问
public WheelLayoutManager WheelLayoutManager => _wheelLayoutManager;
```

## 🔄 模式切换流程

### 初始化流程
```
1. Awake() → InitializeModeManagers() → LoadModeFromConfig()
2. Initialize() → StartListening() → EnableSystemAfterInit()
3. EnableSystemAfterInit() → _activeManager.Initialize()
```

### 切换流程
```
1. 用户触发切换 (F9键或API调用)
2. 切换配置并保存
3. 清空当前轮盘显示
4. 停用当前管理器
5. 切换到新管理器
6. 启用新管理器
7. 显示切换提示
```

## 📊 模式对比

| 特性 | 配件系统模式 | 主背包模式 |
|------|------------|----------|
| 数据源 | 主背包 + 配件物品 | 仅主背包物品 |
| 管理器 | AttachmentWheelManager | MainBackpackWheelManager |
| 启用条件 | 装备支持的背包 | 任何背包都可用 |
| 快捷键 | 自动更新到4/5/Q/G | 保持原版设置不变 |
| 轮盘容量 | 每个类别最多8个 | 总共最多8个 |
| 位置管理 | 配件系统管理 | 背包位置映射 |

## 🎮 用户体验

### 模式切换提示
- 切换时显示清晰的消息提示
- 提示中包含当前模式和切换方式
- 3秒后自动消失

### 无缝切换
- 轮盘立即清空并重新填充
- 配置自动保存，下次启动保持
- 所有状态正确隔离

### 兼容性保证
- 原有配件系统功能完全保留
- 新的主背包模式独立运行
- 两种模式互不干扰

## 🔧 扩展点

### 1. 自定义模式切换
可以添加更多模式：
```csharp
public enum SystemMode
{
    AttachmentSystem,
    MainBackpack,
    // 未来可扩展：混合模式、自定义模式等
}
```

### 2. 模式切换事件
```csharp
public static event Action<SystemMode> OnModeChanged;
```

### 3. 模式特定配置
```csharp
public class ModeConfig
{
    public bool EnableAutoSwitch { get; set; }
    public float NotificationDuration { get; set; }
    public bool EnableModeIndicators { get; set; }
}
```

## ⚠️ 注意事项

### 1. 状态隔离
- 两种模式的状态完全独立
- 切换时必须正确清理和初始化
- 避免状态污染

### 2. 资源管理
- 正确的管理器生命周期
- 事件订阅和取消订阅
- 内存泄漏防护

### 3. 异常处理
- 管理器创建失败处理
- 切换过程中的异常恢复
- 用户友好的错误提示

### 4. 性能考虑
- 避免频繁的模式切换
- 切换过程中的性能优化
- 轮盘刷新的批量处理

## 🚀 未来扩展

### 1. 更多模式支持
- **混合模式**：同时支持配件和主背包
- **自定义模式**：用户自定义的数据源
- **智能模式**：根据背包类型自动选择

### 2. 高级配置
- 模式切换快捷键自定义
- 模式切换条件和规则
- 模式间的数据同步

### 3. 可视化增强
- 模式指示器
- 切换动画效果
- 模式特定UI元素

## 📋 实现总结

✅ **已完成**：
1. 模式管理器的创建和初始化
2. 配置持久化机制
3. 模式切换核心逻辑
4. 背包变化事件委托
5. 公共接口设计
6. 资源清理和生命周期管理

✅ **架构优势**：
1. 清晰的接口抽象
2. 完全的状态隔离
3. 灵活的扩展机制
4. 向后兼容性保证

**状态**：✅ **完成** - BackpackShortcutManager 现在完全支持两种模式的切换，可以作为模式切换控制器使用。