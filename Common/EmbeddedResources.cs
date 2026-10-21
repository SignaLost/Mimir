using System.IO;
using System.Reflection;

namespace Mimir.Common;

/// <summary>Reads files compiled into this assembly.</summary>
internal static class EmbeddedResources
{
    /// <summary>The resource bytes, or null, logged, when it is not embedded.</summary>
    internal static byte[] Read(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

        if (stream == null)
        {
            Log.LogError($"Embedded resource {name} not found.");
            return null;
        }

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
