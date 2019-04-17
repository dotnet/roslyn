using System.Xml.Linq;

namespace BuildBoss
{
    internal readonly struct InternalsVisibleTo
    {
        public InternalsVisibleTo(string targetAssembly, string publicKey, string loadsWithinVisualStudio)
        {
            TargetAssembly = targetAssembly;
            PublicKey = publicKey;
            LoadsWithinVisualStudio = loadsWithinVisualStudio;
        }

        public string TargetAssembly { get; }
        public string PublicKey { get; }
        public string LoadsWithinVisualStudio { get; }

        public override string ToString()
        {
            var element = new XElement("InternalsVisibleTo");
            if (TargetAssembly is object)
                element.Add(new XAttribute("Include", TargetAssembly));
            if (PublicKey is object)
                element.Add(new XAttribute("Key", PublicKey));
            if (LoadsWithinVisualStudio is object)
                element.Add(new XAttribute("LoadsWithinVisualStudio", LoadsWithinVisualStudio));

            return element.ToString();
        }
    }
}
