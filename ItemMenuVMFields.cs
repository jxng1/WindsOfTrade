using System.Reflection;

namespace WindsOfTrade
{
    internal class ItemMenuVMFields
    {
        private const BindingFlags _ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private object _instance;

        public ItemMenuVMFields(object itemMenuVMInstance)
        {
            _instance = itemMenuVMInstance;
        }

        public void SetValue(string field, object value)
        {
            FieldInfo fieldInfo = _instance.GetType().GetField(field, _ALL_FLAGS);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(_instance, value);
            }
        }

        public object GetValue(string field)
        {
            FieldInfo fieldInfo = _instance.GetType().GetField(field, _ALL_FLAGS);

            return fieldInfo != null ? fieldInfo.GetValue(_instance) : null;
        }


    }
}
