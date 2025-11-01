using UnityEngine;

namespace Backpack_QuickWheel
{
    public static class ReflectionExtensions
    {
        public static void SetPrivateField(this object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogError($"Field {fieldName} not found in {obj.GetType().Name}");
            }
        }

        public static T GetPrivateField<T>(this object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return (T)field.GetValue(obj);
            }
            else
            {
                Debug.LogError($"Field {fieldName} not found in {obj.GetType().Name}");
                return default(T);
            }
        }
    }
}
