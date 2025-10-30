using UnityEngine;
using System.Collections;
using Duckov.UI;              // NotificationText
using Duckov.UI.DialogueBubbles; // DialogueBubblesManager

namespace VoiceWheelSystem
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

            switch (type)
            {
                case BubbleType.Notification:
                    ShowNotificationBubble(text);
                    break;
                case BubbleType.Dialogue:
                    ShowDialogueBubble(text);
                    break;
                default:
                    ShowNotificationBubble(text);
                    break;
            }

            Debug.Log($"[VoiceBubbleManager] 显示{type}气泡: {text}");
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
        /// 使用 DialogueBubblesManager.Show()
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

                // 使用游戏的DialogueBubblesManager系统
                // 参数参考: CharacterMainControl.cs:1136
                // DialogueBubblesManager.Show(text, base.transform, yOffset, false, false, speed, 2f).Forget();
                // 使用游戏的NotificationText系统作为替代
                NotificationText.Push(text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceBubbleManager] 显示对话气泡失败: {e.Message}");
                // 回退到通知气泡
                ShowNotificationBubble(text);
            }
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
        /// 参考游戏现有的获取方式
        /// </summary>
        private Transform GetPlayerTransform()
        {
            // 方法1: 通过标签查找
            var playerByTag = GameObject.FindWithTag("Player");
            if (playerByTag != null)
            {
                return playerByTag.transform;
            }

            // 方法2: 查找CharacterMainControl组件
            var characterControl = FindObjectOfType<CharacterMainControl>();
            if (characterControl != null)
            {
                return characterControl.transform;
            }

            // 方法3: 查找CharacterController
            var characterController = FindObjectOfType<CharacterController>();
            if (characterController != null)
            {
                return characterController.transform;
            }

            // 方法4: 通过Camera找到主角色（某些游戏的实现方式）
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 某些游戏中角色是Camera的父级
                var cameraParent = mainCamera.transform.parent;
                if (cameraParent != null)
                {
                    return cameraParent;
                }
            }

            Debug.LogError("[VoiceBubbleManager] 无法找到玩家角色Transform");
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