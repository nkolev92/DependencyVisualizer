using System.Reflection;

namespace SharedUtility
{
    public class TestHelpers
    {
        public static string GetResource(string name, Type type)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(type);

            var manifestResourceStream = type.GetTypeInfo().Assembly.GetManifestResourceStream(name);
            if (manifestResourceStream != null)
            {
                using StreamReader reader = new StreamReader(manifestResourceStream);
                return reader.ReadToEnd();
            }
            else
            {
                throw new ArgumentException($"Cannot find the resource {name}");
            }
        }
    }
}