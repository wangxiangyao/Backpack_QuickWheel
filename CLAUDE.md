# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此代码库中工作时提供指导。

## 项目概述

这是一个为游戏《逃离鸭科夫》开发的 Unity Mod，添加了背包配件系统。该 Mod 允许玩家为现有背包添加配件插槽，并创建各种配件物品（侧面包、战术包、子弹袋等），支持嵌套插槽功能（配件中还有配件）。

## 构建与开发命令

### 构建 Mod
```bash
# 构建项目（输出到游戏的 Mods 目录）
dotnet build Great_backpack.csproj
```

输出路径已在 `.csproj` 文件中自动配置为：`D:\steam\steamapps\common\Escape from Duckov\Duckov_Data\Mods\Great_backpack\`

### 开发配置
- 目标框架：.NET Standard 2.1
- 游戏路径：通过 `.csproj` 中的 `DuckovPath` 变量配置
- 依赖项标记为 `<Private>false</Private>` 以防止复制到输出目录

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

## 架构说明

### 初始化流程

Mod 在 `ModBehaviour.cs` 中遵循严格的初始化顺序：

1. **Awake()**：初始化 Harmony 补丁和本地化系统
2. **InitializeBackpackSystem()** (延迟 2 秒执行)：
   - 通过 `TagManager` 创建所需的标签
   - 导出标签（仅开发模式）
   - 通过 `AttachmentManager` 创建配件物品
   - 通过 `BackpackModifier` 修改现有背包
   - 通过 `BackpackShortcutManager` 初始化快捷键系统

### 核心系统

#### 1. 标签系统 (`TagSystem/`)
- **TagManager**：为配件类型和插槽限制创建自定义标签
- 所有自定义标签使用前缀以避免与游戏标签冲突
- 系统标签（来自游戏）单独加载用于插槽限制

#### 2. 配件系统 (`AttachmentSystem/`)
- **AttachmentManager**：通过克隆游戏基础物品（ID 135 或 1255）创建配件物品
- **AttachmentItemConfig**：每个配件物品的配置数据
- **SlotConfig**：定义插槽类型，包含显示名称和限制标签
- 配件可以拥有嵌套插槽（配件中的配件）

#### 3. 背包修改系统 (`BackpackSystem/`)
- **BackpackModifier**：为现有背包（TypeID 36-40）添加配件插槽
- 每种背包类型获得不同的插槽配置

#### 4. 快捷键系统 (`ShortcutSystem/`)
- **BackpackShortcutManager**：管理键盘快捷键以快速使用物品
- **ItemCategorizer**：物品分类（医疗、兴奋剂、食物、爆炸物）
- **BackpackItemCollector**：递归收集背包和配件中的物品
- **ItemUsageHandler**：处理快捷键触发时的物品使用
- 仅在玩家装备背包时激活

#### 5. 轮盘选择系统 (`ShortcutSystem/`)
**九宫格轮盘UI**：长按快捷键时显示8向轮盘，通过鼠标移动快速选择物品

**核心组件**：
- **InputInterceptor**：拦截快捷键，检测长按（0.2秒阈值）和释放，记录按下时鼠标位置
- **ItemWheelSelector**：轮盘选择器主类，管理九宫格布局和矢量选择逻辑
- **WheelItemDisplay**：单个格子UI组件，负责显示物品icon和聚焦效果（放大+变亮）
- **WheelUIAssets**：资源加载管理器，支持嵌入式grid_bg.png图片

**矢量选择算法**：
- 轮盘中心 = 按下时的鼠标位置（固定不变）
- 每帧计算矢量 = 当前鼠标位置 - 轮盘中心
- 死区阈值 = 20像素（FIRST_VECTOR_THRESHOLD）
- 矢量长度 < 20像素：不做选择
- 矢量长度 >= 20像素：根据矢量方向选中相应格子

**八向布局**：
```
[0] 左上(225°)    [1] 上(270°)    [2] 右上(315°)
[3] 左(180°)      [  中心  ]      [4] 右(0°)
[5] 左下(135°)    [6] 下(90°)     [7] 右下(45°)
```

**交互流程**：
1. 按下快捷键 → 记录按下时鼠标位置为轮盘中心
2. 0.2秒后轮盘显示
3. 用户移动鼠标 → 每帧计算矢量，当长度超过死区时选中物品
4. 释放快捷键 → 使用选中的物品并关闭轮盘

### Harmony 补丁 (`Patches/`)

#### SlotCheckPatch
- 补丁 `Slot.CheckAbleToPlug()` 以实现标签限制的 OR 逻辑
- 仅影响自定义插槽（带有 `wxy_` 前缀的插槽）
- 允许匹配任意限制标签的物品（而非游戏默认的 AND 逻辑）
- 对于多标签插槽（如"Large"可接受钥匙或注射器或食物等）至关重要

#### UIInputManagerPatch & EquipmentControllerPatch (ShortcutSystem)
- 拦截快捷键的键盘输入
- 通知 `BackpackShortcutManager` 背包装备变化

### 配置系统

所有配置数据集中在 `BackpackModConfig.cs` 中：

- **BackpackTypeIDs**：要修改的游戏背包 ID（36-40）
- **SlotConfigs**：旧版插槽类型定义（保留以兼容）
- **UnifiedSlotTypes**：现代插槽类型系统，支持多标签限制
- **BackpackSlotConfigs**：将背包 ID 映射到其插槽布局
- **AttachmentItemConfigs**：所有配件物品的完整定义

### 本地化系统 (`Localization/`)

- **LocalizationManager**：管理多语言支持
- **LocalizationData**：嵌入在每个 `AttachmentItemConfig` 中
- **LanguageDetector**：开发工具，用于导出支持的语言
- 当前支持：简体中文（zh-CN）和英语（en-US）

### 重要实现细节

#### 自定义插槽命名
- 所有自定义插槽使用前缀 `wxy_` + 插槽类型 + 索引
- 示例：`wxy_Small_1`、`wxy_Large_2`
- `SlotCheckPatch` 使用此前缀识别自定义插槽

#### 物品创建流程
1. 克隆基础物品（简单物品用 135，容器用 1255）
2. 通过反射设置属性（typeID、weight、value、displayName）
3. 添加配件标签（如 "SidePocket_Small"）
4. 如果物品是容器则配置插槽
5. 从嵌入资源设置自定义图标
6. 注册到 `ItemAssetsCollection`

#### 反射使用
- `ReflectionExtensions.cs` 提供辅助方法
- 用于设置游戏对象的私有字段
- 必需的，因为游戏的 Item 类有许多私有字段

#### 资源加载
- `ResourceLoader.cs` 处理嵌入式精灵图加载
- 纹理嵌入在程序集中并在运行时加载
- 命名约定：`Textures.ItemName.png`

## 开发注意事项

### 调试
- 使用 `#if DEBUG` 块实现仅开发环境的功能
- 在 Debug 构建中自动导出标签
- `TagExporter.CheckSpecificTags()` 验证所需标签是否存在

### 添加新配件
1. 在 `BackpackModConfig.cs` 的 `AttachmentItemConfigs` 中添加条目
2. 使用 `UnifiedSlotTypes` 中的插槽以保持一致性
3. 为 zh-CN 和 en-US 提供本地化文本
4. 在 `.csproj` 中将自定义图标作为资源嵌入
5. 使用 349100-349999 范围内的唯一 TypeID

### 标签限制
- 插槽限制标签必须与游戏的系统标签完全匹配（区分大小写）
- 常用标签："key"、"SpecialKey"、"Injector"、"Healing"、"Drink"、"Food"、"Explosive"、"Magazine"、"MeleeWeapon"
- 多个标签创建 OR 逻辑（物品匹配任意标签即可）

### 已知限制
- 背包 TypeID 硬编码（36-40）
- 配件 TypeID 必须手动分配且唯一
- 每次初始化都会导出标签（性能影响）
- 需要 2 秒延迟等待游戏物品系统初始化

### 已解决案例

#### ✅ 医疗物品使用失败时被拿起 (已修复)
**提交**: 34e7353
**问题**：角色满血时按医疗快捷键，物品直接被拿到手上而不是显示"无法使用"

**根本原因**：
- `TryUseItemDirectly()` 中，当 `IsUsable()` 返回 false 时，调用了 `EquipItemToHand()` 备选方案
- 这与官方 `UseItem()` 的逻辑不符（官方直接返回并显示提示）

**解决方案**：
- 移除了 `EquipItemToHand()` 的备选逻辑
- 当物品不可使用时，直接调用 `NotificationText.Push("UI_Item_NotUsable")` 显示提示
- 物品保持在快捷栏，与手雷逻辑一致

**关键代码变更**：
- `ShortcutSystem/ItemUsageHandler.cs`: TryUseItemDirectly() 方法

---

#### ✅ 配件卸下后快捷键重新出现 (已修复)
**提交**: fa8b824
**问题**：配件从背包卸下后，配件中物品拿到库存时又在快捷栏重新出现

**根本原因**：
- 官方快捷键系统的 `items[]` 数组仍保存对旧物品的引用
- 当配件卸下后，官方系统未被通知重新验证物品有效性
- 物品移到库存时，`IsItemValid()` 检查通过（因为物品现在在库存中），被重新显示

**解决方案**：
- 在 `RefreshItems()` 末尾添加 `NotifyOfficialShortcutSystemToValidate()` 调用
- 该方法通过反射获取官方 `ItemShortcut.OnSetItem` 事件
- 为所有快捷栏位触发该事件，让官方系统重新验证所有物品
- 配件中不在库存的物品被清除

**关键代码变更**：
- `ShortcutSystem/BackpackShortcutManager.cs`:
  - `RefreshItems()` 方法添加通知调用
  - 新增 `NotifyOfficialShortcutSystemToValidate()` 方法

---

#### ✅ 禁用官方快捷键设置功能 (已实现)
**提交**: d017d64
**功能**：禁止官方快捷键系统对我们管理的快捷键进行手动设置

**问题背景**：
- 用户在库存中hover物品，然后按快捷键按钮
- 官方系统会自动将hover的物品设置到快捷栏
- 这会覆盖我们通过背包收集得到的物品

**解决方案**：
- 使用Patch拦截 `ItemShortcut.Set()` 方法
- 当快捷键系统启用时，检查设置的快捷键索引
- 如果是我们管理的快捷键（Index 0-3），返回false禁止设置
- 允许官方系统继续管理后两个快捷键（Index 4-5）

**关键代码变更**：
- 新增 `ShortcutSystem/Patches/ItemShortcutSetPatch.cs`:
  - Patch `ItemShortcut.Set()` 方法
  - 检查 `BackpackShortcutManager.IsShortcutSystemEnabled && index < 4`

---

#### ✅ 九宫格轮盘UI系统 (已实现)
**提交**: d61b8bc, 96586ac
**功能**：长按快捷键时显示九宫格轮盘，通过鼠标移动矢量快速选择物品

**设计思路**：
- 轮盘中心固定在按下快捷键时的鼠标位置
- 基于矢量方向的选择方式，比Hover更高效
- 20像素死区阈值，避免误触

**矢量选择优化历程**：
1. 初版：使用两个矢量（矢量1：按下到显示、矢量2：显示后的鼠标移动），导致选择中心点在0.2s后位置
2. 优化：改为轮盘中心使用按下时位置，只使用单一矢量
3. 最终版：每帧根据当前鼠标位置和固定的轮盘中心计算矢量选择

**关键代码变更**：
- 新增 `ShortcutSystem/InputInterceptor.cs`：长按检测和快捷键拦截
- 新增 `ShortcutSystem/ItemWheelSelector.cs`：轮盘显示和矢量选择逻辑
- 新增 `ShortcutSystem/WheelItemDisplay.cs`：单个格子的UI和聚焦效果
- 修改 `Textures/grid_bg.png`：嵌入式格子背景图片
- `Great_backpack.csproj`：添加嵌入资源配置

**调整参数**：
- `LONG_PRESS_THRESHOLD = 0.2f`：轮盘显示时间阈值
- `FIRST_VECTOR_THRESHOLD = 20f`：矢量选择死区大小（像素）
- `HOVER_SCALE = 1.15f`：选中格子的放大倍数
- `ANIMATION_DURATION = 0.1f`：聚焦效果动画时长

---

#### ✅ 快捷键数据源分离与配件物品UI更新 (已修复)
**提交**: 7831133
**问题**：物品放入配件后，快捷键 UI 不更新，轮盘拖动后布局重置

**根本原因**：
1. **数据源混淆**：使用单一的 `_categorizedItems` 混合存储两个不同的用途
   - 物品清单（库存物品）
   - 轮盘布局（用户自定义排列）
   - 这导致 `null` 占位符污染整个系统，每个方法都需要特殊的 null 处理
2. **协程等待错误**：在 `DelayedIncementalUpdate` 中使用 `WaitForSeconds(0.01f)` 不足以等待数据更新完成
3. **分类逻辑错误**：尝试对配件容器本身进行分类，但容器不属于任何 ItemCategory

**解决方案**：
- **分离数据源**：
  - `_categorizedItems`：保持干净，仅包含实际物品（无 null）
  - `_wheelLayouts`：保存用户布局，包含 null 占位符
- **修复协程等待**：改用 `yield return StartCoroutine(IncrementalUpdateCategorizedItems())` 确保完全等待
- **修复 UI 更新逻辑**：当配件内容变化时，进行全量快捷键 UI 更新（因为配件内可能有多种类别物品）
- **改正数据查找**：`SetCurrentSelection()` 在轮盘布局中查找物品索引，而非 _categorizedItems
- **数据流优化**：`GetItemsForCategory()` 优先返回用户布局，确保 null 占位符的一致性

**关键代码变更**：
- `ShortcutSystem/BackpackShortcutManager.cs`:
  - 添加 `_wheelLayouts` 独立数据结构（第 21 行）
  - `IncrementalUpdateCategorizedItems()` 两步更新：_categorizedItems + _wheelLayouts（第 335-432 行）
  - `SetCurrentSelection()` 改为在轮盘布局中查找（第 847-882 行）
  - `DelayedIncementalUpdate()` 正确等待协程并全量更新 UI（第 296-313 行）
  - 删除过时的 `UpdateRelatedShortcutUI()` 方法

**架构设计要点**：
```
数据流：
背包物品变化 → OnAttachmentContentChanged → IncrementalUpdateCategorizedItems
   ↓
   ├─ 步骤1：保持 _categorizedItems 干净（无 null）
   └─ 步骤2：独立更新 _wheelLayouts（保留 null）
   ↓
GetItemsForCategory() 合并数据（优先级：_wheelLayouts > _categorizedItems）
   ↓
UpdateShortcutUIForCategory() 显示合并后的数据
```

**经验总结**：
- ❌ **不要混用不同用途的数据结构**：分离关注点，让每个数据源有清晰的单一职责
- ❌ **不要使用 null 作为数据混淆**：null 占位符必须隔离在专门的数据结构中
- ✅ **协程等待必须完整**：使用 `yield return StartCoroutine()` 而非 `WaitForSeconds`
- ✅ **事件回调参数**：仔细确认回调参数的含义（配件内容变化时是容器，不是变化的物品）

---

### 已知问题

#### ✅ 配件事件重复订阅导致的无限循环 (已修复)
**提交**: eb2558e
**问题**：打开背包UI时，控制台反复打印"获取轮盘布局"和"物品列表"，形成无限循环

**根本原因**：
- `SubscribeToAttachmentsChanges()` 方法在被调用时未检查是否已经订阅过
- 当背包UI打开导致多次调用该方法时，会对同一配件的 `onChildChanged` 事件重复注册回调
- 事件被触发时，回调函数被多次执行，导致 `UpdateShortcutUI()` 被多次调用
- 形成事件-更新-事件的无限循环

**解决方案**：
- 在 `SubscribeToAttachmentsChanges()` 中添加检查 `if (!_subscribedAttachments.Contains(slot.Content))`
- 防止对已订阅的配件进行重复订阅
- 与 `OnBackpackContentChanged()` 中的做法保持一致

**关键代码变更**：
- `ShortcutSystem/BackpackShortcutManager.cs`: SubscribeToAttachmentsChanges() 方法添加重复订阅检查

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

## 功能待做清单

### P0 - 关键问题（立即修复）
- [x] 修复玩家总负重计算错误（其实是正确的，误判）
- [x] 修复配件事件重复订阅导致的无限循环

### P1 - 核心功能（本周期重点）
- [ ] 配件设置基础属性（重量、价值、稀有度等）
- [ ] 配件添加Icon
- [ ] 轮盘布局持久化
- [ ] 配件附加效果（如减少负重）
- [ ] 点击配件展开配件插槽UI，可拖拽放入物品

### P2 - 扩展功能（下一阶段）
- [ ] 修复钥匙在配件中无法使用的问题（需要与其他系统交互）
- [ ] 近战武器接入轮盘系统
- [ ] 轮盘添加物品信息（耐久、堆叠数量、物品名称）
- [ ] 物品放入轮盘优先放入左右上下四个格子
- [ ] 当前选中物品格子左上角添加绿色圆点指示

### P3 - 优化与趣味（可选）
- [ ] 处理轮盘超过8个物品的方案
- [ ] 优化行军背包配件槽位显示拥挤问题
- [ ] 不可使用物品多次使用会生气功能（生气气泡文字）
- [ ] 添加特定快捷键，按下时发出"噶"声并吸引敌人
