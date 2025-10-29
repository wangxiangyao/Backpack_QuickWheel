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

---

#### ✅ 轮盘布局持久化 (已实现)
**提交**: 870c375
**功能**：将用户调整的轮盘物品位置保存到文件，游戏重启后自动恢复

**问题背景**：
- 用户可以长按快捷键打开轮盘，通过拖拽调整8个格子中的物品位置
- 但游戏重启后这些调整会丢失，每次都要重新排列
- 需要实现轮盘布局的持久化

**核心设计决策**：
1. **使用位置而非物品身份**：物品没有唯一ID（InstanceID会在重启时改变），改为使用 `(配件槽位索引, 物品槽位索引)` 作为唯一标识
2. **手工JSON生成/解析**：Unity的 `JsonUtility` 不支持数组序列化和嵌套自定义类，改为手工生成和使用括号计数法解析
3. **位置验证**：加载时验证保存的位置是否有效（配件是否存在、物品是否在该位置），验证失败则放弃恢复

**关键难点与解决**：

*难点1：InstanceID在游戏重启后改变*
- ❌ 初版方案：保存物品的 InstanceID
- 结果：游戏重启后所有InstanceID都不同，无法查找物品
- ✅ 解决：改用位置标识 `(attachmentSlotIndex, itemSlotIndex)`

*难点2：JsonUtility无法序列化数组和嵌套类*
- ❌ 尝试：用 `[Serializable]` 嵌套类 + Array
- 结果：JSON 生成的只有 `{"savedTimestamp": ...}`，categories 为空
- ✅ 解决：手工生成 JSON 字符串，避开 JsonUtility 限制

*难点3：JSON 解析中的嵌套括号问题*
- ❌ 初版方案：用非贪心正则 `\[(.*?)\]` 提取数组
- 问题：遇到第一个 `]` 就停止，导致 itemLocations 数组的 `]` 被误认为是 categories 的结尾
- ✅ 解决：使用**括号计数法**替代正则，能正确处理嵌套的 `[]` 和 `{}`

**相关文件**：

1. **WheelLayoutData.cs** (新建)
   - `ItemLocation`：记录单个物品位置（配件槽位索引 + 物品槽位索引）
   - `CategoryLayout`：某个分类的布局（分类名 + 物品位置数组）
   - `WheelLayoutData`：完整轮盘布局（所有分类 + 时间戳）

2. **WheelLayoutPersistence.cs** (新建)
   - `ConvertToData()`：将内存中的轮盘布局转换为序列化数据
   - `RestoreFromData()`：将保存的数据恢复为轮盘布局，验证位置有效性
   - `GenerateJson()`：手工生成 JSON，支持数组和嵌套结构
   - `ParseJson()`：使用括号计数法手工解析 JSON
   - `GetItemLocation()`：找出物品在背包-配件层级中的位置
   - `FindItemByLocation()`：根据位置查找物品对象

3. **BackpackShortcutManager.cs** (修改)
   - `SubscribeToBackpackChanges()` 中添加调用 `LoadPersistedWheelLayouts()`
   - 添加 `PersistWheelLayouts()` 公开方法用于保存

4. **InputInterceptor.cs** (修改)
   - `OnShortcutKeyUp()` 中轮盘关闭前添加 `PersistWheelLayouts()` 调用

**持久化流程**：
```
轮盘显示
    ↓
用户拖拽调整物品
    ↓
快捷键释放 → InputInterceptor.OnShortcutKeyUp()
    ↓
关闭轮盘前 → PersistWheelLayouts()
    ↓
ConvertToData() → GenerateJson() → SaveToFile()
    ↓
游戏重启
    ↓
背包装备 → BackpackShortcutManager.SubscribeToBackpackChanges()
    ↓
LoadPersistedWheelLayouts()
    ↓
LoadFromFile() → ParseJson() → RestoreFromData() → 验证位置 → 恢复布局
    ↓
轮盘显示时使用恢复的布局
```

**验证与调试**：
- 保存时日志：打印转换的分类数、每个分类的位置数、JSON 大小
- 加载时日志：打印文件大小、解析后的分类数、每个分类的位置数、位置验证结果
- 失败时日志：如果任何物品位置验证失败，弃用整个布局并清空

**经验总结**：
- ❌ **不要依赖 InstanceID**：游戏重启时会改变，改用游戏内位置标识
- ❌ **不要盲目相信序列化框架**：JsonUtility 有很多限制，简单情况手工序列化更可靠
- ✅ **嵌套结构必须括号计数**：正则非贪心匹配无法处理嵌套括号，计数法更稳定
- ✅ **加载前必须验证数据**：检查引用是否有效，位置是否超界，分类是否匹配

---

#### ✅ 配件品质体系与价格设计（P1任务）
**日期**：2025-10-29
**决策**：建立配件与官方背包相对应的品质和价格体系

**官方背包参考数据**：
```
品质等级  DisplayQuality  官方背包名称    Value   定位
   1     White          装饰包         87     基础品质
   2     Green          小学背包       338    初级配件
   3     Blue           旅行包         995    中级配件
   4     Purple         生存者背包     2385   高级配件
   5     Orange         行军背包       4760   顶级配件
   6     Red            (无)          5000+  超凡品质
```

**DisplayQuality枚举体系**（共9种）：
```
None(0), White(1), Green(2), Blue(3), Purple(4), Orange(5), Red(6), Q7(7), Q8(8)
```

**配件品质分配方案**：

| 品质 | 配件名称 | TypeID | 重量 | 价格 | 获取难度 | 说明 |
|------|---------|--------|------|------|---------|------|
| 2 | 网兜 | 349100 | 0.3kg | 145 | 极易 | 最早期必需配件 |
| 2 | 小钥匙袋 | 349130 | 0.2kg | 85 | 极易 | 钥匙初级方案 |
| 3 | 水壶袋 | 349101 | 0.4kg | 675 | 容易 | 食物+小物件 |
| 3 | 战术小透明 | 349131 | 0.5kg | 785 | 容易 | 战术小包升级 |
| 4 | 工具箱 | 349140 | 1.2kg | 2050 | 中等 | 大型容器配件 |
| 5 | 嘎嘎战术腰带 | 349151 | 0.35kg | 4850 | 困难 | 两个挂钩的高级战术腰带 |
| 5 | 零重力肩带 | 349160 | 0.4kg | 5680 | 困难 | 减重肩带（后续可添加减重效果） |
| 5 | 手机袋 | 349170 | 0.18kg | 4950 | 困难 | 肩带包（肩带是高级背包专有） |
| 6 | 嘎嘎收纳包 | 349120 | 0.7kg | 8200 | 非常困难 | 行军背包独有大包 |
| 6 | 嘎嘎战术包 | 349141 | 1.0kg | 9350 | 非常困难 | 行军背包战术配置专用 |
| 6 | 战术子弹袋 | 349180 | 0.6kg | 7100 | 非常困难 | 行军背包弹匣神器 |

**价格逻辑**：
- 品质2-3：远低于官方背包价格（目标：逐步接近）
- 品质4：接近官方生存者包价格(2385)范围
- 品质5：几乎持平行军背包价格(4760)，5000元左右
- 品质6：远高于行军背包价格，7000-9000+元

**设计原则**：
1. **品质与背包等级绑定**：低品质配件对应低等级背包的插槽，高品质配件独占高等级背包
2. **快捷键系统优先**：锁扣和肩带等与快捷键有关的配件提升品质等级
3. **嵌套容量对价值**：更多插槽=更高价格，为快捷键系统提供差异化价值
4. **价格有零有整**：避免单调的整数定价，提高沉浸感

**后续配件设计规则**：
- 新增配件必须严格按照此体系分配品质和价格
- 品质顺序：Green(2)→Blue(3)→Purple(4)→Orange(5)→Red(6)
- 定价时先确定品质，再根据重量、插槽数量、获取难度调整具体价格
- 品质高于5的配件必须有充分的设计理由（极稀有、超级功能等）

**代码修改**：
- `AttachmentItemConfig.cs`：添加 `Quality` 和 `DisplayQuality` 属性
- `BackpackModConfig.cs`：为全部12个配件分配了品质、价格、重量

---

#### ✅ 配件描述文案优化（P1任务）
**日期**：2025-10-29
**风格定位**：俏皮有趣 + 功能提示 + 稀有度体现

**设计原则**：
1. **拟人化与比喻** - 用有趣的表达而不是枯燥的功能列表
2. **隐含功能提示** - 通过描述暗示玩家应该放什么东西
3. **稀有度递进** - 低品质朴实，高品质专业/霸气
4. **双语自然** - 中英文都保持俏皮风格

**12个配件描述列表**：

| 品质 | 配件名称 | 中文描述 | 英文描述 |
|------|---------|---------|---------|
| Green | 网兜 | 装瓶水？还是一个萝卜？反正食物就行～ | A bottle of water? A carrot? Anything food works~ |
| Green | 小钥匙袋 | 钥匙的家，装满了就都堵门口吧 | Keys' home. Fill it up and you'll never lose one! |
| Blue | 水壶袋 | 能放点吃喝，还能放个钥匙、针剂，就没地儿了。。 | Room for some snacks and drinks, maybe a key and syringe... but then it's full. |
| Blue | 战术小透明 | 透明材质，小物件一目了然，专业人士的秘密武器 | Crystal clear visibility. Perfect for organizing those small essentials at a glance! |
| Purple | 工具箱 | 行动必备！医疗包、水、粮食...这箱子就是你的移动仓库 | Your mobile supply depot! Medical kits, water, rations... pack it all in! |
| Orange | 嘎嘎战术腰带 | 专业级战术腰带，两个挂钩稳稳地固定你的武器和装备。行动中的好搭档 | Professional-grade tactical belt with dual hooks to secure your weapons and gear. The perfect companion for action! |
| Orange | 零重力肩带 | 仿佛背的不是物资，而是空气。你的肩膀会感谢你 | Feels like carrying air, not supplies. Your shoulders will thank you! |
| Orange | 手机袋 | 名叫手机袋，其实啥小东西都能装。钥匙、针剂、糖果...顺手一掏 | Called a phone pocket but holds everything small. Keys, syringes, candy... grab and go! |
| Red | 嘎嘎收纳包 | 诺亚方舟级收纳！大小物件都能装，这才是真正的整理大师 | Noah's Ark of storage! Everything finds its place. The master organizer! |
| Red | 战术子弹袋 | 弹匣杀手！四个弹夹齐排队。火力全开从它开始 | Magazine heaven! Four mags ready to roll. Non-stop firepower begins here! |
| Red | 嘎嘎战术包 | 终极之选！手雷、装备、补给...最专业的战术配置尽在其中 | The ultimate choice! Grenades, gear, supplies... pure tactical perfection! |

**说明**：
- 这是初版文案，暂未涉及具体的游戏效果（如减重、加速）
- 等配件特殊功能全部实现后（P1后期），会再次修改描述以体现具体效果
- 比如零重力肩带会改为"能减XX%负重"，提示玩家具体效果

---

## 配件插槽可视化与快速编辑功能实现计划

**总体目标**：提供直观的配件槽位编辑界面，让玩家无需卸下配件就能快速查看和调整槽位内容

**官方UI参考**：
- `SlotCollectionDisplay.cs` - 官方槽位显示组件，已能显示和交互
- `ItemDetailsDisplay.cs` - 官方物品详情面板，已支持槽位显示
- `ItemOperationMenu.cs` - 官方右键菜单框架
- `KontextMenu.cs` - 官方上下文菜单通用框架

---

### 阶段1：MVP（本周期完成）

#### ✅ 任务1.1：Hover显示槽位信息
**日期**：2025-10-30 **状态**：已完成

**功能**：鼠标Hover配件物品时，tooltip显示所有槽位信息

**实现方案**：
1. 创建 `AttachmentUIHelper.cs` - 槽位信息生成工具类
   - `IsAttachment(Item)` - 判断物品是否为配件（有插槽）
   - `GetAttachmentSlotsTooltip(Item)` - 生成槽位信息文本
   - `GetAttachmentSlotUsage(Item)` - 返回"已用/总数"格式
   - `GetAttachmentFullInfo(Item)` - 返回完整信息

2. 创建 `AttachmentHoveringUIManager.cs` - Hover事件管理
   - 订阅 `ItemHoveringUI.onSetupItem` 事件
   - 通过反射获取hover面板的`itemDescription`文本组件
   - 在原有描述后追加槽位信息

3. 修改 `ModBehaviour.cs`
   - 在Awake()中添加`AttachmentHoveringUIManager.Initialize()`调用

**显示效果**：
```
[配件槽位 2/4]
Slot 1: Item A x2
【绿色】Slot 2: (key、SpecialKey、Injector)
Slot 3: Item B
【绿色】Slot 4: (任意)
```

**设计决策**：
- 空槽位名称显示绿色，一眼看出哪些是空的
- Tag列表从第二个开始显示（第一个就是槽位名称本身）
- 简化文案为`(tag1、tag2、...)`格式，避免排版拥挤

---

#### ⏳ 任务1.2：点击配件跳转到详情面板
**计划**：2-3小时

**功能**：点击背包中的配件物品 → 中间详情面板显示该配件的完整信息和插槽

**实现方案**：
- 利用官方现有的 `ItemDetailsDisplay.Setup(Item target)` 方法
- 该方法已支持显示物品槽位和 `SlotCollectionDisplay`
- 需要找到点击物品的入口点，可能需要：
  - Patch `ItemDisplay.OnPointerClick()` 或相关点击逻辑
  - 或利用现有的 `ItemDisplay.OnItemClicked` 事件（如果存在）
  - 触发 `ItemDetailsDisplay.Setup(clickedItem)`

**关键代码位置**：
- `GameSource/Duckov/ItemDetailsDisplay.cs` (第103-137行) - 已能显示配件槽位
- `GameSource/Duckov/SlotCollectionDisplay.cs` - 槽位显示和交互组件
- `GameSource/Duckov/ItemDisplay.cs` - 物品显示和点击逻辑

**预期效果**：
- 点击配件 → 右侧详情面板显示配件名、描述、槽位列表
- 可直接拖拽库存物品到配件槽位中
- 无需卸下配件即可快速编辑

---

#### ✅ 任务1.3：UI辅助类创建
**日期**：2025-10-30 **状态**：已完成

**功能**：为后续任务提供槽位检查和信息获取的工具方法

**实现**：已包含在任务1.1的 `AttachmentUIHelper.cs` 中

---

### 阶段2：完善（下周期）

#### ⏳ 任务2.1：右键返回上一详情（历史栈）
**计划**：2小时

**功能**：维护详情浏览历史，右键返回上一个物品的详情

**实现方案**：
- 创建 `AttachmentUIHistory` 类维护详情浏览历史栈
- 每次打开物品详情时，将前一个物品压入历史栈
- 右键点击时弹出栈顶，显示前一个物品详情
- 或在详情面板上添加"返回"按钮

**相关组件**：
- `ItemDetailsDisplay.cs` - 需要修改以支持右键返回
- `KontextMenu.cs` - 可用于右键菜单

---

#### ⏳ 任务2.2：圆孔高亮拖拽反馈
**计划**：需先测试UI交互方式

**功能**：用户拖拽物品到配件槽位时，目标槽位显示高亮效果（圆孔变色/放大）

**实现方案**：
- 获取 `SlotIndicator` 组件（显示槽位的圆孔UI）
- 在拖拽事件中动态修改：
  - Material/Shader参数实现放大效果
  - Color实现高亮变色
- 或通过修改 `Image.color` 属性改变透明度/亮度

**关键代码位置**：
- `GameSource/Duckov/SlotIndicator.cs` - 槽位显示器组件
- `GameSource/Duckov/ItemDisplay.cs` - 拖拽事件逻辑

---

### 实现优先级

| 任务 | 优先级 | 难度 | 时间 | 依赖 | 状态 |
|------|--------|------|------|------|--------|
| 1.1 Hover显示信息 | P0 | 低 | 2-3h | ItemHoveringUI ✅ | ✅ 完成 |
| 1.2 点击跳转详情 | P0 | 低 | 2-3h | ItemDetailsDisplay ✅ | ⏳ 待实现 |
| 1.3 UI辅助类 | P0 | 极低 | 1h | 无 | ✅ 完成 |
| 2.1 右键返回历史 | P1 | 低 | 2h | 1.2完成后 | ⏳ 待实现 |
| 2.2 圆孔高亮反馈 | P1 | 中 | 需测试 | SlotIndicator ✅ | ⏳ 待实现 |

---

### 创建文件清单

**已创建**：
- ✅ `AttachmentUI/AttachmentUIHelper.cs` - 槽位信息工具类
- ✅ `AttachmentUI/AttachmentHoveringUIManager.cs` - Hover管理器

**计划创建**：
- ⏳ `AttachmentUI/Patches/ItemDisplayClickPatch.cs` - 点击物品Patch（如需）
- ⏳ `AttachmentUI/AttachmentUIHistory.cs` - 详情浏览历史栈
- ⏳ `AttachmentUI/SlotHighlightHelper.cs` - 槽位高亮效果辅助类

---

#### ✅ 钥匙在配件插槽中无法使用的问题 (已修复)
**提交**: 2b44ba9
**日期**: 2025-10-30
**问题**：将钥匙放入配件的插槽中时，玩家接近上锁的箱子/门仍显示"需要钥匙"，无法打开

**根本原因分析**（调试过程中发现的关键认知错误）：

❌ **错误理解1：数据层级的二元论**
- **初始假设**：Item可能放在两个地方：`Inventory` 或 `Slots`
- **实际情况**：官方系统的设计有三层结构，但关键是理解每层的**所有权关系**
- **正确认知**：
  - 已装备的背包存在于 `Character.CharacterItem.Slots` 中
  - 背包内的物品存在于 `Backpack.Slots` 和 `Backpack.Inventory` 中
  - **Inventory链是断开的**：一旦物品被放入Slots，其`InInventory`被设为null

❌ **错误理解2：搜索入口的方向**
- **初始尝试**：从 `Character.CharacterItem.Inventory` 开始遍历
- **问题**：装备已经穿上的背包不在Inventory中，而在Slots中！
- **正确方向**：应该从 `Character.CharacterItem.Slots` 开始，遍历已装备的物品

❌ **错误理解3：Item.Inventory的有效性**
- **初始判断**：配件物品应该有Inventory来存放放入其中的物品
- **实际结构**：放入配件中的物品存在于配件的`Slots`中，不是`Inventory`
- **正确做法**：只递归检查`Slots`，不检查`Inventory`（已清空）

**解决方案**：
- 创建Harmony Patch拦截 `InteractableBase.TryGetRequiredItem()`
- **搜索流程**（正确的逐层递归）：
  1. 遍历 `Character.CharacterItem.Slots` 获取已装备的物品（包括背包）
  2. 对每个装备物品递归调用 `SearchInSlots()`
  3. 检查其Slots中的物品及其Slots中的嵌套物品（配件的配件）
  4. 同时检查 `Character.CharacterItem.Inventory` 中的未装备物品及其Slots

**关键代码变更**：
- 新增 `Patches/InteractableTryGetRequiredItemPatch.cs`:
  - Postfix Patch，如果原始方法未找到物品则继续搜索
  - 正确的搜索顺序：装备Slots → 递归嵌套Slots → 库存物品Slots
  - 递归函数 `SearchInSlots()` 处理嵌套配件的情况

**核心代码示意**：
```csharp
// 错误方向（已弃用）：
foreach (Item item in fromCharacter.CharacterItem.Inventory) // ❌ 装备物品不在这里

// 正确方向：
foreach (Slot slot in fromCharacter.CharacterItem.Slots) // ✅ 从装备槽位开始
{
    // 递归检查这个物品及其嵌套物品的Slots
    SearchInSlots(slot.Content, requiredItemId);
}

// 递归搜索
private static ValueTuple<bool, Item> SearchInSlots(Item item, int requireItemId)
{
    if (item?.Slots == null) return (false, null);

    foreach (Slot slot in item.Slots)
    {
        if (slot.Content?.TypeID == requireItemId)
            return (true, slot.Content);  // 找到了

        // 继续递归（配件中的配件）
        var nested = SearchInSlots(slot.Content, requireItemId);
        if (nested.Item1) return nested;
    }

    return (false, null);
}
```

**调试经历与教训**：

1. **反射获取字段的陷阱**
   - ❌ 尝试用反射获取private字段 `GetField("requireItem", BindingFlags.NonPublic | ...)`
   - 结果：找不到，因为它们是public字段
   - ✅ 修正：直接访问public字段 `__instance.requireItem`

2. **日志驱动的调试效能**
   - 添加详细日志跟踪每一步：是否进入Patch、搜索了哪些物品、递归深度等
   - 日志输出让问题变得"可见"，快速识别搜索方向的错误

3. **理解官方架构的必要性**
   - 必须查阅官方源码理解 `Character.CharacterItem.Slots` vs `Backpack.Inventory` 的关系
   - 不能基于"应该怎样"的假设，必须基于"实际怎样"的代码

**经验总结**：
- ❌ **不要假设数据结构**：必须通过源码验证数据在哪里
- ❌ **不要仅修改一个地方**：钥匙搜索需要递归检查多层嵌套
- ✅ **从外向内递推**：从已知的Character.CharacterItem.Slots开始，逐层向内
- ✅ **日志一切**：关键的数据访问都应该有可追踪的日志
- ✅ **测试完整路径**：验证单层、双层、三层嵌套的情况

---

#### ✅ 轮盘布局恢复逻辑健壮性提升 (已修复)
**提交**: a7db4be
**日期**: 2025-10-30
**问题**：医疗物品和针剂在快捷键上不显示，轮盘布局被意外改变后无法正确恢复

**根本原因**：
旧的轮盘布局恢复机制存在以下缺陷：
1. 恢复失败检查不够全面 - 只在物品找不到时弃用，但无法发现全是空位的情况
2. 缺乏完整性验证 - 即使所有物品都恢复失败，布局仍被认为有效
3. 关注点混淆 - 恢复和验证逻辑混在一起，难以调试

**解决方案**：
分离恢复和验证职责，建立**一一对应检查机制**

1. **修改 RestoreFromData()**（仅负责恢复）
   - 从文件读取数据后逐一恢复
   - 即使物品找不到也继续恢复（不提前返回）
   - 返回恢复的布局（可能包含null或不匹配的物品）

2. **新增 ValidateLayoutIntegrity()**（专门负责验证）
   ```csharp
   // 规则：轮盘布局中的物品必须与收集的物品完全一一对应
   对于每个分类（Medical、Stim、Food等）：
   1. 计算恢复的非null物品数
   2. 计算收集的物品数
   3. 如果数量不相等 → 返回false（弃用布局）
   4. 遍历每个恢复的物品
   5. 如果有任何物品不在收集列表中 → 返回false（弃用布局）
   ```

3. **更新加载流程**
   ```
   LoadPersistedWheelLayouts()
     ↓
   1. 从文件加载数据
     ↓
   2. 恢复布局（RestoreFromData）
     ↓
   3. 验证完整性（ValidateLayoutIntegrity）
     ├─ 通过 → 使用布局 ✅
     └─ 失败 → 弃用，生成默认布局 ❌
   ```

**关键代码变更**：
- `ShortcutSystem/WheelLayoutPersistence.cs`:
  - 修改 `RestoreFromData()` 仅负责恢复
  - 新增 `ValidateLayoutIntegrity()` 验证完整性
- `ShortcutSystem/BackpackShortcutManager.cs`:
  - 修改 `LoadPersistedWheelLayouts()` 实施恢复-验证二步流程

**同步日志优化**：
- `BackpackItemCollector.cs`: 保留物品收集的关键日志，便于追踪
- `BackpackShortcutManager.cs`: 删除过度详细的UI更新日志
- `WheelLayoutPersistence.cs`: 简化恢复过程的日志

**效果**：
- ✅ 医疗物品和针剂正确显示在快捷键上
- ✅ 轮盘布局自动弃用机制更健壮
- ✅ 恢复失败时自动使用默认生成的布局
- ✅ 代码关注点清晰，易于维护和扩展

**经验总结**：
- ✅ **分离关注点**：恢复和验证是两个不同的职责
- ✅ **完整性检查**：数据恢复后必须进行有效性验证
- ✅ **一一对应原则**：持久化数据必须与当前数据完全对应
- ✅ **优雅降级**：验证失败时使用默认方案，而不是死坚持

---

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
- [x] 配件设置基础属性（重量、价值、稀有度等）
- [x] 配件添加Icon
- [x] 轮盘布局持久化及完整性验证
- [x] 配件Hover面板显示槽位信息（带颜色区分空/满槽位）
- [x] 修复钥匙在配件中无法使用的问题
- [x] 修复医疗物品和针剂不显示的问题
- [ ] 配件附加效果（如减少负重）
- [ ] 点击配件展开配件插槽UI，可拖拽放入物品

### P2 - 扩展功能（下一阶段）
- [ ] 修复新引入的bug（如ItemShortcutIsItemValidPatch的边界情况）
- [ ] 近战武器接入轮盘系统
- [ ] 轮盘添加物品信息（耐久、堆叠数量、物品名称）
- [ ] 物品放入轮盘优先放入左右上下四个格子
- [ ] 当前选中物品格子左上角添加绿色圆点指示

### P3 - 优化与趣味（可选）
- [ ] 处理轮盘超过8个物品的方案
- [ ] 优化行军背包配件槽位显示拥挤问题
- [ ] 不可使用物品多次使用会生气功能（生气气泡文字）
- [ ] 添加特定快捷键，按下时发出"噶"声并吸引敌人
