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
