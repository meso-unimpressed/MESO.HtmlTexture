using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vanadium.Core
{
    public static class Globals
    {
        public static readonly string AssemblyLocation = typeof(HtmlTextureStartable).Assembly.Location;
        public static readonly string AssemblyDir = Path.GetDirectoryName(AssemblyLocation) ?? "C:\\cef";
        public static readonly string LivePagePath = Path.Combine(Globals.AssemblyDir, "livepage");

        public static string LivePageLoader;
        public static string LivePageUnloader;

        public static readonly Dictionary<string, string> ScriptsFromOnloadFolder = new Dictionary<string, string>();

        static Globals()
        {
            UpdateScripts();
        }

        public static void UpdateScripts()
        {
            LivePageLoader =
                File.ReadAllText(Path.Combine(LivePagePath, "load.js")) +
                File.ReadAllText(Path.Combine(LivePagePath, "live_resource.js")) +
                File.ReadAllText(Path.Combine(LivePagePath, "livepage.js"));
            LivePageUnloader = File.ReadAllText(Path.Combine(LivePagePath, "unload.js"));

            ScriptsFromOnloadFolder.Clear();
            foreach (var file in Directory.GetFiles(Path.Combine(AssemblyDir, "onloadjs")))
            {
                ScriptsFromOnloadFolder.Add(file, File.ReadAllText(file));
            }
        }
    }
}
