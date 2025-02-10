using System.Reflection;

namespace WindsOfTrade
{
    internal class ItemMenuVMFields
    {
        private const BindingFlags _ALL = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private object instance;

        public ItemMenuVMFields(object itemMenuVMInstance)
        {
            instance = itemMenuVMInstance;
        }

        public void SetValue(string field, object value)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(field, _ALL);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(instance, value);
            }
        }

        public object GetValue(string field)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(field, _ALL);

            return fieldInfo != null ? fieldInfo.GetValue(instance) : null;
        }


    }
}
