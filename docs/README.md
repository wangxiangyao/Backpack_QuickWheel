# 项目文档中心

**项目**: 逃离鸭科夫 - 背包配件Mod
**状态**: 活跃开发中
**更新时间**: 2025-10-31

## 📁 文档结构

### 🚀 01-core/ - 核心文档
最重要和最常用的文档，新成员必读。

- **[DEVELOPMENT.md](01-core/DEVELOPMENT.md)** - 开发指南和核心原则
- **[TODO.md](01-core/TODO.md)** - 任务清单和进度跟踪
- **[CHANGELOG.md](01-core/CHANGELOG.md)** - 版本更新记录

### 🏗️ 02-architecture/ - 架构设计
系统架构和结构设计文档。

- **[project-structure.md](02-architecture/project-structure.md)** - 项目架构说明
- **[PROJECT_STRUCTURE-项目结构总结.md](02-architecture/PROJECT_STRUCTURE-项目结构总结.md)** - 详细结构分析

### 📋 03-planning/ - 计划文档
功能规划、复杂任务分析和设计方案。

- **[ui-features.md](03-planning/ui-features.md)** - UI功能规划
- **[voice-wheel.md](03-planning/voice-wheel.md)** - 语音轮盘开发计划
- **[active-plans/](03-planning/active-plans/)** - 当前进行中的复杂任务规划
- **[analyses/](03-planning/analyses/)** - 问题分析文档归档
- **[designs/](03-planning/designs/)** - 解决方案设计文档归档

**任务规划指南**: 参考 `memory/task-planning-best-practices.md`

### 🔧 04-technical/ - 技术设计
具体功能的技术实现文档。

- **[hover-color-optimization.md](04-technical/hover-color-optimization.md)** - 物品hover颜色优化
- **[drag-system-fixes.md](04-technical/drag-system-fixes.md)** - 拖拽系统修复
- **[voice-bubble-optimization.md](04-technical/voice-bubble-optimization.md)** - 语音气泡优化

### 🧠 memory/ - 记忆体系
跨会话记忆库，包含技术细节和经验总结。

- **[README.md](memory/README.md)** - 记忆库索引
- **[technical-principles.md](memory/technical-principles.md)** - 技术原则和核心守则
- **[success-stories.md](memory/success-stories.md)** - 成功案例汇总
- **[common-pitfalls.md](memory/common-pitfalls.md)** - 常见陷阱和错误
- **[item-usage-guide.md](memory/item-usage-guide.md)** - Item类使用指南

## 🎯 文档使用指南

### 新成员入门
1. 阅读 `../CLAUDE.md`（项目主要记忆入口）
2. 阅读 `01-core/DEVELOPMENT.md` 了解开发原则
3. 查看 `01-core/TODO.md` 了解当前任务
4. 根据需要查阅其他分类文档

### 开发过程中
- **遇到技术问题**: `memory/technical-principles.md` + `memory/common-pitfalls.md`
- **寻找解决方案**: `memory/success-stories.md` + `04-technical/`
- **了解功能规划**: `03-planning/`
- **查看系统架构**: `02-architecture/`

### 任务完成后
严格执行**三层持久化方法论**:
1. **Git提交**: 记录代码变更
2. **项目文档**: 更新 `01-core/TODO.md` 和 `01-core/CHANGELOG.md`
3. **技术文档**: 创建/更新 `04-technical/` 中的对应文档

## 📝 文档维护原则

### 分类标准
- **01-core**: 必须频繁访问的核心文档
- **02-architecture**: 系统级设计文档，相对稳定
- **03-planning**: 功能规划，随开发进展更新
- **04-technical**: 具体实现细节，每个功能一份
- **memory**: 跨会话记忆，按类型分类存储

### 命名规范
- 使用英文命名，便于版本控制
- 使用kebab-case格式：`feature-name.md`
- 避免中英文混用
- 保持简洁且描述性强

### 内容原则
- **单一职责**: 每个文档专注一个主题
- **易于检索**: 通过清晰的分类快速找到
- **及时更新**: 功能完成后立即更新相关文档
- **避免重复**: 通过引用替代内容重复

## 🔍 快速查找

| 需求 | 文档位置 |
|------|----------|
| 开发原则 | `01-core/DEVELOPMENT.md` |
| 当前任务 | `01-core/TODO.md` |
| 技术守则 | `memory/technical-principles.md` |
| 成功案例 | `memory/success-stories.md` |
| 常见错误 | `memory/common-pitfalls.md` |
| Item使用 | `memory/item-usage-guide.md` |
| 具体功能实现 | `04-technical/` |
| 功能规划 | `03-planning/` |
| 系统架构 | `02-architecture/` |

---
**维护规则**: 分类清晰，命名统一，内容及时更新，避免重复冗余