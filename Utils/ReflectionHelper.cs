using System.Reflection;
using MelonLoader;

namespace BonelabMultiplayerMockup.Utils
{
    public class ReflectionHelper
    {
        public static T GetPrivateField<T>(object fieldHolder, string fieldName)
        {
            if (fieldHolder == null) return default;

            var type = fieldHolder.GetType();
            var fieldInfo =
                type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                MelonLogger.Error("Attempted to get a private field which does not exist.");
                return default;
            }

            return (T)fieldInfo.GetValue(fieldHolder);
        }
    }
}