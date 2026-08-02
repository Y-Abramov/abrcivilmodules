using System.Collections.Generic;

namespace AbrCivil.Modules.Core
{
    /// <summary>Запись каталога модулей линейки.</summary>
    internal class ModuleEntry
    {
        public string Name = "";
        public string Title = "";
        public string Version = "";
        public string Description = "";
        public string BundleUrl = "";
        public string Sha256 = "";
        public string HelpUrl = "";
        public List<string> Compatibility = new List<string>();
    }
}
