using UnityEngine;

namespace Great_backpack
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
    }
}
