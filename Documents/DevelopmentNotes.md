# 开发笔记

## 项目结构说明

### 核心系统
- `ModBehaviour.cs` - Mod主入口
- `BackpackModConfig.cs` - 所有配置数据
- 采用数据与逻辑分离的架构

### 子系统
- `AttachmentSystem/` - 配件物品管理
- `BackpackSystem/` - 背包修改功能  
- `TagSystem/` - Tag创建和管理

## 已知问题

1. TODO: 需要研究游戏中的物品Tag系统
2. TODO: 实现配件效果应用到背包
3. TODO: 添加配件物品图标

## 开发计划

### 短期目标
- [ ] 完成配件效果系统
- [ ] 研究游戏Tag限制

### 长期目标
- [ ] 添加更多配件类型
- [ ] 实现可视化配置界面
- [ ] 支持多语言