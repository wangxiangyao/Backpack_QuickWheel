# 文档系统使用指南

## 🎯 文档系统概述

项目采用分层分类的文档体系，确保信息组织清晰、易于检索和维护。

**文档总览**: `../README.md` - 完整的文档索引和使用指南

## 📁 文档分类详解

### 🚀 01-core/ - 核心文档
**用途**: 最重要和最常用的文档，新成员必读
**访问频率**: 每日使用
**更新频率**: 随开发进展频繁更新

**关键文档**:
- `DEVELOPMENT.md` - 开发指南和核心原则
- `TODO.md` - 当前任务清单和进度
- `CHANGELOG.md` - 版本更新历史

**使用场景**:
- 新成员入门培训
- 每日开发工作参考
- 任务进度跟踪

### 🏗️ 02-architecture/ - 架构设计
**用途**: 系统级设计文档，提供整体架构理解
**访问频率**: 中等（设计阶段和问题排查时）
**更新频率**: 较低（架构相对稳定）

**关键文档**:
- `project-structure.md` - 系统架构和组件关系
- `file-structure.md` - 项目文件目录结构

**使用场景**:
- 理解系统整体设计
- 新功能架构规划
- 问题排查时的系统理解

### 📋 03-planning/ - 计划文档
**用途**: 功能规划和开发计划
**访问频率**: 中等（规划阶段和任务分配时）
**更新频率**: 中等（随规划调整更新）

**关键文档**:
- `ui-features.md` - UI功能规划
- `voice-wheel.md` - 语音轮盘开发计划

**使用场景**:
- 功能规划讨论
- 开发任务分配
- 进度计划制定

### 🔧 04-technical/ - 技术设计
**用途**: 具体功能的技术实现细节
**访问频率**: 高（开发和问题解决时）
**更新频率**: 高（每个功能完成时）

**关键文档**:
- `hover-color-optimization.md` - 物品hover颜色优化
- `drag-system-fixes.md` - 拖拽系统修复
- `voice-bubble-optimization.md` - 语音气泡优化

**使用场景**:
- 具体功能实现参考
- 类似问题解决方案查找
- 技术细节查阅

### 🧠 memory/ - 记忆体系
**用途**: 跨会话记忆库，技术细节和经验总结
**访问频率**: 很高（每次会话都会使用）
**更新频率**: 高（经验积累时）

**关键文档**:
- `technical-principles.md` - 技术原则和核心守则
- `success-stories.md` - 成功案例汇总
- `common-pitfalls.md` - 常见陷阱和错误
- `item-usage-guide.md` - Item类使用指南

**使用场景**:
- 跨会话上下文恢复
- 技术问题解决
- 经验参考和学习

## 🎯 场景化使用指南

### 新成员入门流程
```
1. 阅读 ../CLAUDE.md（主要记忆入口）
2. 查看 ../README.md（文档总览）
3. 阅读 01-core/DEVELOPMENT.md（开发原则）
4. 查看 01-core/TODO.md（当前任务）
5. 根据需要查阅 memory/technical-principles.md
```

### 复杂任务处理流程
```
接收复杂任务时:
1. 识别任务复杂度（>4小时，多文件，新技术？）
2. 查阅 memory/task-planning-best-practices.md
3. 执行任务开始三重分析：
   - 问题理解分析 → docs/03-planning/analyses/
   - 解决方案规划 → docs/03-planning/designs/
   - 任务分解执行 → docs/01-core/TODO.md
4. 开始执行子任务
```

### 日常开发工作流
```
开始工作:
1. 查看 01-core/TODO.md（今日任务）
2. 阅读相关 04-technical/ 文档（技术参考）
3. 查阅 memory/common-pitfalls.md（避免陷阱）

遇到问题:
1. 查阅 memory/technical-principles.md（技术原则）
2. 搜索 memory/success-stories.md（类似解决方案）
3. 查看 04-technical/（具体实现细节）
4. 参考 memory/item-usage-guide.md（API使用）

完成任务:
1. 严格执行三层持久化方法论
2. 更新 01-core/TODO.md 和 01-core/CHANGELOG.md
3. 创建/更新 04-technical/ 中对应文档
```

### 功能开发流程
```
规划阶段:
1. 查看 03-planning/（功能规划）
2. 阅读 02-architecture/（架构约束）

实现阶段:
1. 查阅 04-technical/（类似实现参考）
2. 参考 memory/technical-principles.md（技术原则）
3. 避免 memory/common-pitfalls.md 中的陷阱

完成后:
1. 更新 04-technical/（技术文档）
2. 执行三层持久化方法论
```

### 问题排查流程
```
技术问题:
1. 查看 memory/common-pitfalls.md（常见错误）
2. 搜索 memory/success-stories.md（成功案例）
3. 查阅 02-architecture/（系统理解）
4. 检查 04-technical/（相关实现）

API使用问题:
1. 查看 memory/item-usage-guide.md（Item类使用）
2. 阅读 memory/technical-principles.md（技术原则）
3. 搜索相关源码 GameSource/Duckov/
```

## 🔍 快速查找索引

| 需求类型 | 主要文档 | 备用文档 |
|---------|----------|----------|
| **当前任务** | `01-core/TODO.md` | - |
| **复杂任务规划** | `memory/task-planning-best-practices.md` | `03-planning/active-plans/` |
| **开发原则** | `memory/technical-principles.md` | `01-core/DEVELOPMENT.md` |
| **技术守则** | `memory/technical-principles.md` | - |
| **成功案例** | `memory/success-stories.md` | `04-technical/` |
| **常见错误** | `memory/common-pitfalls.md` | - |
| **Item使用** | `memory/item-usage-guide.md` | - |
| **具体实现** | `04-technical/` | `memory/success-stories.md` |
| **系统架构** | `02-architecture/` | - |
| **功能规划** | `03-planning/` | - |
| **版本历史** | `01-core/CHANGELOG.md` | - |

## ⚡ 快速访问技巧

### 按使用频率排序
1. **最高频**: `01-core/TODO.md`, `memory/technical-principles.md`
2. **高频**: `memory/common-pitfalls.md`, `memory/success-stories.md`
3. **中频**: `04-technical/`, `01-core/DEVELOPMENT.md`
4. **低频**: `02-architecture/`, `03-planning/`

### 按问题类型排序
- **"我该做什么?"** → `01-core/TODO.md`
- **"这怎么实现?"** → `04-technical/` + `memory/success-stories.md`
- **"有什么坑?"** → `memory/common-pitfalls.md`
- **"正确做法?"** → `memory/technical-principles.md`
- **"系统架构?"** → `02-architecture/`
- **"未来规划?"** → `03-planning/`

### 文档间引用关系
```
memory/technical-principles.md ← 01-core/DEVELOPMENT.md
memory/success-stories.md ← 04-technical/
memory/common-pitfalls.md ← 所有实现文档
memory/item-usage-guide.md ← 04-technical/ + 源码
```

## 📝 文档维护最佳实践

### 作为使用者
- **优先搜索**: 使用快速查找索引定位文档
- **遵循引用**: 文档间的引用指向更详细信息
- **反馈问题**: 发现文档问题时及时反馈
- **贡献经验**: 将新经验添加到对应记忆文档

### 作为维护者
- **保持同步**: 代码变更后及时更新相关文档
- **检查引用**: 确保文档间引用关系正确
- **版本记录**: 重要更新在CHANGELOG中记录
- **定期整理**: 定期检查和优化文档结构

## 🎯 记忆要点

1. **文档总览**: `../README.md` 是文档系统的入口
2. **核心记忆**: `../CLAUDE.md` 是跨会话的主要入口
3. **任务查询**: `01-core/TODO.md` 是当前任务的唯一来源
4. **技术守则**: `memory/technical-principles.md` 包含最高优先级守则
5. **快速查找**: 使用场景化指南快速定位所需文档

---
**更新时间**: 2025-10-31
**重要性**: 高 - 每次会话都会使用文档系统
**使用原则**: 先查索引，再读详细，遵循引用链