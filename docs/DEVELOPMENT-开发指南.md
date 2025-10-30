# 开发指南

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
