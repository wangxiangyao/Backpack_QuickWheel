using UnityEngine;
using System.Collections;
using Duckov.UI;              // NotificationText
using Duckov.UI.DialogueBubbles; // DialogueBubblesManager

namespace Backpack_QuickWheel.VoiceWheelSystem
{
    /// <summary>
    /// 语音气泡显示管理器
    /// 复用游戏现有的气泡系统：NotificationText 和 DialogueBubblesManager
    /// </summary>
    public class VoiceBubbleManager : MonoBehaviour
    {
        #region 气泡类型配置
        public enum BubbleType
        {
            Notification,    // 屏幕通知 (NotificationText)
            Dialogue         // 角色头顶气泡 (DialogueBubblesManager)
        }

        [Header("气泡设置")]
        public BubbleType defaultBubbleType = BubbleType.Dialogue;

        [Header("对话气泡参数")]
        private float DIALOGUE_BUBBLE_HEIGHT = 2f;   // 角色头顶高度偏移
        private float DIALOGUE_SPEED = 1f;           // 显示速度
        private float DIALOGUE_DURATION = 2f;        // 显示时长
        #endregion

        #region 组件引用
        private Transform _playerTransform;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            // 获取玩家角色Transform
            _playerTransform = GetPlayerTransform();
        }
        #endregion

        #region 气泡显示接口
        /// <summary>
        /// 显示气泡文字
        /// </summary>
        /// <param name="text">要显示的文字</param>
        /// <param name="bubbleType">气泡类型</param>
        public void ShowBubble(string text, BubbleType? bubbleType = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning("[VoiceBubbleManager] 气泡文本为空");
                return;
            }

            var type = bubbleType ?? defaultBubbleType;

            Debug.LogError($"[VoiceBubbleManager] === 显示气泡开始 === 文本: '{text}', 类型: {type}");

            switch (type)
            {
                case BubbleType.Notification:
                    Debug.Log("[VoiceBubbleManager] 选择通知气泡");
                    ShowNotificationBubble(text);
                    break;
                case BubbleType.Dialogue:
                    Debug.Log("[VoiceBubbleManager] 选择对话气泡");
                    ShowDialogueBubble(text);
                    break;
                default:
                    Debug.Log("[VoiceBubbleManager] 回退到通知气泡");
                    ShowNotificationBubble(text);
                    break;
            }

            Debug.LogError($"[VoiceBubbleManager] === 显示气泡完成 === {type}: {text}");
        }

        /// <summary>
        /// 显示通知气泡（屏幕通知）
        /// 使用 NotificationText.Push()
        /// </summary>
        private void ShowNotificationBubble(string text)
        {
            try
            {
                // 使用游戏的NotificationText系统
                NotificationText.Push(text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceBubbleManager] 显示通知气泡失败: {e.Message}");
            }
        }

        /// <summary>
        /// 显示对话气泡（角色头顶）
        /// 直接使用游戏官方的CharacterMainControl.PopText方法
        /// </summary>
        private void ShowDialogueBubble(string text)
        {
            try
            {
                if (_playerTransform == null)
                {
                    Debug.LogWarning("[VoiceBubbleManager] 无法找到玩家Transform，回退到通知气泡");
                    ShowNotificationBubble(text);
                    return;
                }

                // 直接使用游戏官方的PopText方法，这是最快的方式
                // 使用超快的速度参数：50f（默认10f，我们用50f超快）
                var playerCharacter = _playerTransform.GetComponent<CharacterMainControl>();
                if (playerCharacter != null)
                {
                    playerCharacter.PopText(text, 50f);  // 超快速度！
                    Debug.Log($"[VoiceBubbleManager] ✓ 使用官方PopText显示气泡（超快速度50f）: {text}");
                }
                else
                {
                    Debug.LogWarning("[VoiceBubbleManager] 无法找到CharacterMainControl组件，回退到通知气泡");
                    ShowNotificationBubble(text);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceBubbleManager] 显示对话气泡失败: {e.Message}");
                // 回退到通知气泡
                ShowNotificationBubble(text);
            }
        }

        /// <summary>
        /// 协程版本的对話氣泡顯示
        /// </summary>
        private System.Collections.IEnumerator ShowDialogueBubbleCoroutine(string text, Transform target)
        {
            // 使用反射调用异步方法
            var dialogueManager = Duckov.UI.DialogueBubbles.DialogueBubblesManager.Instance;
            if (dialogueManager != null)
            {
                var showMethod = typeof(Duckov.UI.DialogueBubbles.DialogueBubblesManager)
                    .GetMethod("Show", new System.Type[] {
                        typeof(string),
                        typeof(Transform),
                        typeof(float),
                        typeof(bool),
                        typeof(bool),
                        typeof(float),
                        typeof(float)
                    });

                bool showSuccess = false;
                if (showMethod != null)
                {
                    try
                    {
                        Debug.Log($"[VoiceBubbleManager] 调用DialogueBubblesManager.Show显示气泡: {text}");

                        var task = showMethod.Invoke(null, new object[] {
                            text,
                            target,
                            2f,       // yOffset - 头顶高度
                            false,    // needInteraction
                            false,    // skippable
                            5f,       // speed - 显示速度（加快显示）
                            2f        // duration - 显示时长
                        });

                        Debug.Log($"[VoiceBubbleManager] ✓ DialogueBubblesManager.Show调用成功");
                        showSuccess = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[VoiceBubbleManager] 调用DialogueBubblesManager.Show失败: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError("[VoiceBubbleManager] 找不到DialogueBubblesManager.Show方法");
                }

                if (showSuccess)
                {
                    // 立即完成，不等待任何时间
                    yield break;
                }
                else
                {
                    // 回退到通知气泡，立即完成
                    ShowNotificationBubble(text);
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning("[VoiceBubbleManager] DialogueBubblesManager实例不存在");
                ShowNotificationBubble(text);
                yield break;
            }

            // 确保协程正常结束
            yield break;
        }

        /// <summary>
        /// 隐藏当前气泡（如果支持的话）
        /// </summary>
        public void HideCurrentBubble()
        {
            // 注意：游戏现有的气泡系统没有提供直接的隐藏接口
            // NotificationText 和 DialogueBubblesManager 都是自动消失的
            // 这里留空，如果需要隐藏功能，可能需要额外的实现
            Debug.Log("[VoiceBubbleManager] 气泡系统不支持手动隐藏，将自动消失");
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取玩家角色Transform
        /// 使用游戏官方的方式：CharacterMainControl.Main
        /// </summary>
        private Transform GetPlayerTransform()
        {
            Debug.LogError("[VoiceBubbleManager] === 开始查找玩家Transform ===");

            // 使用游戏官方的方式获取主角色
            var mainCharacter = CharacterMainControl.Main;
            if (mainCharacter != null)
            {
                Debug.LogError($"[VoiceBubbleManager] ✓ 找到玩家角色: {mainCharacter.gameObject.name}");
                return mainCharacter.transform;
            }

            Debug.LogError("[VoiceBubbleManager] ✗ CharacterMainControl.Main为null");
            return null;
        }

        
        /// <summary>
        /// 更新玩家Transform（当角色切换或重生时）
        /// </summary>
        public void RefreshPlayerTransform()
        {
            _playerTransform = GetPlayerTransform();
        }
        #endregion

        #region 公共配置方法
        /// <summary>
        /// 设置默认气泡类型
        /// </summary>
        public void SetDefaultBubbleType(BubbleType type)
        {
            defaultBubbleType = type;
        }

        /// <summary>
        /// 设置对话气泡参数
        /// </summary>
        public void SetDialogueBubbleParameters(float height, float speed, float duration)
        {
            DIALOGUE_BUBBLE_HEIGHT = height;
            DIALOGUE_SPEED = speed;
            DIALOGUE_DURATION = duration;
        }
        #endregion
    }
}