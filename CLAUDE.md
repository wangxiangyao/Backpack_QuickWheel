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

## 开发交流规则

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
