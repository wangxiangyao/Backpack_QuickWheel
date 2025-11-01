# 测试配置：使用Bullet标签快速验证掉落

## ⚠️ 重要警告

**这是测试配置，不要发布到生产环境！**
- Bullet标签权重90%，配件会大量掉落
- 会破坏游戏平衡
- 仅用于开发测试

## 🎯 目的

通过临时添加Bullet标签（权重90%），可以在1-3次开箱内验证配件掉落功能，而不需要开30+个箱子。

## 📝 测试配置代码

### 修改文件：`src/AttachmentSystem/AttachmentManager.cs`

在`SetAttachmentTag`方法中添加测试标签：

```csharp
private void SetAttachmentTag(Item item, string requiredTag)
{
    // 添加自定义的配件类型标签（用于背包插槽识别）
    if (createdTags.TryGetValue(requiredTag, out Tag attachmentTag))
    {
        item.Tags.Add(attachmentTag);
        Debug.Log($"为物品 {item.DisplayName} 添加配件类型标签: {requiredTag}");
    }

    // 🧪 测试模式：添加Bullet标签（权重90%，极易触发）
    // ⚠️ 测试完成后必须注释掉！
    Tag bulletTag = FindSystemTag("Bullet");
    if (bulletTag != null)
    {
        if (!item.Tags.Contains(bulletTag))
        {
            item.Tags.Add(bulletTag);
            Debug.LogWarning($"⚠️ 测试模式：为物品 {item.DisplayName} 添加Bullet标签（权重90%）");
        }
    }
    else
    {
        Debug.LogError("未找到Bullet标签！");
    }

    // 添加官方的"Accessory"标签（用于掉落系统识别）
    Tag accessoryTag = FindSystemTag("Accessory");
    if (accessoryTag != null)
    {
        // 避免重复添加
        if (!item.Tags.Contains(accessoryTag))
        {
            item.Tags.Add(accessoryTag);
            Debug.Log($"为物品 {item.DisplayName} 添加官方Accessory标签，使其可以从lootbox掉落");
        }
    }
    else
    {
        Debug.LogWarning($"未找到官方Accessory标签，物品 {item.DisplayName} 可能无法从lootbox掉落");
    }
}
```

## 🧪 测试流程

### 步骤1：启用测试配置

1. 打开 `src/AttachmentSystem/AttachmentManager.cs`
2. 添加上面的Bullet标签代码（第11-22行）
3. 编译MOD

### 步骤2：F8验证标签

1. 启动游戏并加载MOD
2. 进入游戏场景
3. 按`F8`键
4. 查看控制台输出

**预期输出**：
```
⚠️ 测试模式：为物品 网兜 添加Bullet标签（权重90%）
为物品 网兜 添加官方Accessory标签，使其可以从lootbox掉落

搜索结果: 找到 XX 个物品
  - 网兜 (TypeID: 349100, Quality: 2)
    标签: [SidePocket_Small, Bullet, Accessory]  ← 关键：同时有Bullet和Accessory
  包含Accessory标签: ✅ 是
```

### 步骤3：实际开箱测试

**推荐测试地点**（包含Bullet标签，权重90%的箱子）：

从`LootBoxDebug.json`找到的Bullet箱子：
```
1. LootBox_Bullet_15
   位置: (324.89, 0.00, 192.50)
   Tags: Explosive(5%), Bullet(90%), Accessory(3%)

2. LootBox_Bullet_*（多个箱子）
   搜索方法：在游戏中找带"Bullet"名称的箱子
```

**测试过程**：
1. 找到Bullet箱子
2. 首次打开
3. 检查是否掉落配件

**预期结果**：
- **91.8%概率**获得配件（Bullet权重90/98）
- 开1-3个箱子就能触发
- 如果开5个箱子都没有配件，说明有bug

### 步骤4：恢复生产配置

**测试成功后，必须移除Bullet标签！**

```csharp
private void SetAttachmentTag(Item item, string requiredTag)
{
    // 添加自定义的配件类型标签（用于背包插槽识别）
    if (createdTags.TryGetValue(requiredTag, out Tag attachmentTag))
    {
        item.Tags.Add(attachmentTag);
        Debug.Log($"为物品 {item.DisplayName} 添加配件类型标签: {requiredTag}");
    }

    // 🔴 删除测试代码 - Bullet标签部分完全移除

    // 添加官方的"Accessory"标签（用于掉落系统识别）
    Tag accessoryTag = FindSystemTag("Accessory");
    if (accessoryTag != null)
    {
        if (!item.Tags.Contains(accessoryTag))
        {
            item.Tags.Add(accessoryTag);
            Debug.Log($"为物品 {item.DisplayName} 添加官方Accessory标签，使其可以从lootbox掉落");
        }
    }
    else
    {
        Debug.LogWarning($"未找到官方Accessory标签，物品 {item.DisplayName} 可能无法从lootbox掉落");
    }
}
```

## 📊 测试结果判断

### ✅ 成功标志

**F8验证**：
- [ ] 配件同时包含 `Bullet` 和 `Accessory` 标签
- [ ] "包含Accessory标签: ✅ 是"
- [ ] Search结果中找到配件

**开箱测试**：
- [ ] 开1-3个Bullet箱子获得配件
- [ ] 配件可以正常使用
- [ ] 配件可以安装到背包插槽

### ❌ 失败情况

| 现象 | 原因 | 解决方法 |
|------|------|----------|
| F8显示"未找到Bullet标签" | 系统tag加载失败 | 检查LoadSystemTags()是否正常执行 |
| 配件没有Bullet标签 | SetAttachmentTag未被调用 | 检查CreateAttachmentItemFromConfig流程 |
| 开10个箱子都没有配件 | Search没有返回配件 | 用F8验证Search结果，检查ItemAssetsCollection注册 |
| 获得配件但无法使用 | 配件创建有其他问题 | 检查插槽、本地化等其他配置 |

## 🔄 其他高权重Tag测试

如果Bullet测试不方便，可以尝试其他高权重tag：

### Tool标签（权重60%）

```csharp
Tag toolTag = FindSystemTag("Tool");
if (toolTag != null && !item.Tags.Contains(toolTag))
{
    item.Tags.Add(toolTag);
    Debug.LogWarning($"⚠️ 测试模式：为物品 {item.DisplayName} 添加Tool标签（权重60%）");
}
```

**优势**：
- 权重60%，依然很高
- Tool箱子更多（33个）
- 更容易找到测试地点

### Daily标签（权重50%）

```csharp
Tag dailyTag = FindSystemTag("Daily");
if (dailyTag != null && !item.Tags.Contains(dailyTag))
{
    item.Tags.Add(dailyTag);
    Debug.LogWarning($"⚠️ 测试模式：为物品 {item.DisplayName} 添加Daily标签（权重50%）");
}
```

**优势**：
- 权重50%，仍然较高
- Daily箱子分布广
- 不会与子弹箱混淆

## 📝 测试检查清单

### 准备阶段
- [ ] 备份当前AttachmentManager.cs
- [ ] 添加测试代码（Bullet标签）
- [ ] 编译MOD无错误
- [ ] 了解如何恢复生产配置

### 验证阶段
- [ ] F8显示配件有Bullet标签
- [ ] F8显示配件有Accessory标签
- [ ] Search结果包含配件
- [ ] 找到Bullet箱子位置

### 测试阶段
- [ ] 开第1个箱子 - 结果：______
- [ ] 开第2个箱子 - 结果：______
- [ ] 开第3个箱子 - 结果：______
- [ ] 配件可以正常使用
- [ ] 配件可以安装到背包

### 清理阶段
- [ ] 移除Bullet标签代码
- [ ] 只保留Accessory标签
- [ ] 重新编译验证
- [ ] F8确认只有Accessory标签
- [ ] 提交代码（生产版本）

## ⚠️ 再次提醒

**测试完成后必须移除Bullet标签！**

- ❌ 不要发布包含Bullet标签的版本
- ❌ 不要提交包含测试代码的git commit
- ✅ 仅在本地开发环境使用
- ✅ 测试完成立即恢复生产配置

## 🎓 学习要点

通过这个测试，你会理解：

1. **Tag匹配机制**：ItemAssetsCollection.Search()如何通过tag筛选物品
2. **权重系统**：RandomContainer如何根据权重分配掉落概率
3. **触发时机**：物品在玩家首次打开箱子时才生成
4. **调试方法**：如何使用F8工具快速验证配置

## 更新历史

- 2025-11-01: 创建测试配置文档
