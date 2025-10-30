using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Backpack_QuickWheel.AttachmentSystem
{
    public static class ResourceLoader
    {
        public static Sprite LoadEmbeddedSprite(string resourcePath, string itemName = "")
        {
            try
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();

                // 资源路径需要包含默认命名空间
                string fullResourcePath = $"Great_backpack.{resourcePath}";

                using (Stream stream = executingAssembly.GetManifestResourceStream(fullResourcePath))
                {
                    if (stream == null)
                    {
                        Debug.LogError($"[背包MOD] 嵌入资源未找到: {fullResourcePath}");

                        // 调试：列出所有嵌入资源
                        string[] resourceNames = executingAssembly.GetManifestResourceNames();
                        Debug.Log("[背包MOD] 可用资源列表:");
                        foreach (string name in resourceNames)
                        {
                            Debug.Log($" - {name}");
                        }
                        return null;
                    }

                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(buffer))
                    {
                        Debug.LogError("[背包MOD] 图片加载失败");
                        return null;
                    }

                    texture.filterMode = FilterMode.Bilinear;
                    texture.Apply();

                    Sprite sprite = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f);

                    Debug.Log($"[背包MOD] 成功加载贴图: {resourcePath}");
                    return sprite;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[背包MOD] 加载贴图出错: {e.Message}");
                return null;
            }
        }
    }
}