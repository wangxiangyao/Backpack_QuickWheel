# VoiceWheelSystem 暂存备份

## 文件清单
- VoiceInputPatch.cs - Harmony补丁，拦截F1/Quack输入
- VoiceAudioManager.cs - 音频播放管理器
- VoiceBubbleManager.cs - 气泡显示管理器
- VoiceInputInterceptor.cs - 输入拦截器
- VoiceItem.cs - 语音项数据结构
- VoiceNPCManager.cs - NPC吸引管理器
- VoiceWheelData.cs - 轮盘数据配置
- VoiceWheelManager.cs - 语音轮盘主控制器
- VoiceWheelSelector.cs - 轮盘UI选择器
- Ga_Sound.mp3 - 默认"嘎"音频文件

## 核心功能
1. F1键触发语音轮盘（复用官方Quack机制）
2. 8槽轮盘UI显示
3. 音频播放 + 气泡显示 + NPC吸引
4. 支持自定义语音配置
5. 嵌入式音频资源管理

## 关键技术点
- 使用Harmony.PatchAll拦截CharacterInputControl.OnQuackInput
- AudioManager.PostCustomSFX播放音频
- NotificationText.Push和DialogueBubblesManager显示气泡
- AIMainBrain.MakeSound吸引NPC
- 嵌入式资源通过EmbeddedResource加载

## 依赖关系
- Duckov.Modding.ModBehaviour (主入口)
- HarmonyLib (补丁系统)
- Duckov (游戏命名空间，包含AudioManager等)
- UnityEngine (UI、音频、GameObject等)

## 初始化顺序
1. ModBehaviour.Awake() 创建基础组件
2. OnLevelInitialized() 初始化VoiceWheelManager等
3. VoiceInputInterceptor 监听输入
4. F1触发 -> 显示轮盘 -> 选择 -> 播放效果

保存时间: 2025-01-31