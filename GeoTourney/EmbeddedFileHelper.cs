using System.IO;
using System.Reflection;

namespace GeoTourney
{
    public class EmbeddedFileHelper
    {
        public static string Content(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using Stream? stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{name}");
            if (stream == null)
            {
                return string.Empty;
            }

            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
    }
}