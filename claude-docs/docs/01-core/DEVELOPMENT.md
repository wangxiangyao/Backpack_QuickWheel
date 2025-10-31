# 开发指南

## 🚀 快速开始

### 构建命令
```bash
dotnet build Backpack_QuickWheel.csproj
```
*输出到游戏目录：`D:\steam\steamapps\common\Escape from Duckov\Duckov_Data\Mods\Backpack_QuickWheel\`*

### 开发环境
- **框架**: .NET Standard 2.1
- **配置**: 游戏路径通过 `.csproj` 的 `DuckovPath` 变量设置
- **调试**: 使用 `#if DEBUG` 块，自动导出标签验证

## 📝 开发规范

### 添加新配件
1. 在 `BackpackModConfig.cs` 的 `AttachmentItemConfigs` 中配置
2. 使用 `UnifiedSlotTypes` 插槽类型保持一致性
3. 提供中英文本地化文本
4. 在 `.csproj` 中嵌入图标资源
5. 分配唯一 TypeID（范围：349100-349999）

### 标签系统
- **严格匹配**: 标签必须与游戏系统标签完全一致（区分大小写）
- **常用标签**: `"key"`, `"SpecialKey"`, `"Injector"`, `"Healing"`, `"Drink"`, `"Food"`, `"Explosive"`, `"Magazine"`, `"MeleeWeapon"`
- **OR逻辑**: 多个标签创建匹配条件（物品匹配任意标签即可）

## ⚠️ 技术约束

### 已知限制
- 背包 TypeID 硬编码（36-40）
- 配件 TypeID 需手动分配且唯一
- 初始化时标签导出（性能影响）
- 需要 2 秒延迟等待游戏物品系统初始化

### 开发原则
- **源码驱动**: 任何技术问题必须先查看 `GameSource/Duckov/` 源码
- **官方API优先**: 使用游戏现有API，避免重复造轮子
- **简洁实现**: 避免过度复杂的协程和反射

## 📚 重要文档参考

- **源码位置**: `GameSource/Duckov/` - 游戏官方源码
- **技术原则**: `../memory/technical-principles.md` - 详细技术守则
- **经验总结**: `../memory/key-experiences.md` - 项目核心经验
- **任务规划**: `../memory/task-planning-best-practices.md` - 复杂任务分析方法
- **文档使用**: `../memory/documentation-system-guide.md` - 文档系统使用指南

## 🔄 工作流程

### 任务开始（复杂任务）
1. 识别任务复杂度（>4小时，多文件，新技术？）
2. 执行任务开始三重分析
3. 更新当前TODO清单

### 任务执行
1. 按照设计方案实现
2. 遇到问题时查阅相关记忆文档
3. 遵循源码驱动开发原则

### 任务完成
严格执行三层持久化方法论：
1. **Git提交记录** - 代码变更历史
2. **项目文档更新** - TODO和CHANGELOG更新
3. **技术设计文档** - 完整实现细节

---

**维护原则**: 源码驱动，简洁实现，完整记录

**📖 详细方法论**:
- 三层持久化完整指南: `../memory/technical-principles.md`
- 任务规划最佳实践: `../memory/task-planning-best-practices.md`
- 元认知自动化机制: `../memory/meta-cognition-patterns.md`