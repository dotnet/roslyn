using System.Xml.Linq;

namespace BuildBoss
{
    internal readonly struct InternalsVisibleTo
    {
        public InternalsVisibleTo(string targetAssembly, string publicKey, string loadsWithinVisualStudio, string workItem)
        {
            TargetAssembly = targetAssembly;
            PublicKey = publicKey;
            LoadsWithinVisualStudio = loadsWithinVisualStudio;
            WorkItem = workItem;
        }

        public string TargetAssembly { get; }
        public string PublicKey { get; }
        public string LoadsWithinVisualStudio { get; }
        public string WorkItem { get; }

        public override string ToString()
        {
            var element = new XElement("InternalsVisibleTo");
            if (TargetAssembly is object)
            {
                element.Add(new XAttribute("Include", TargetAssembly));
            }

            if (PublicKey is object)
            {
                element.Add(new XAttribute("Key", PublicKey));
            }

            if (LoadsWithinVisualStudio is object)
            {
                element.Add(new XAttribute("LoadsWithinVisualStudio", LoadsWithinVisualStudio));
            }

            if (WorkItem is object)
            {
                element.Add(new XAttribute("WorkItem", WorkItem));
            }

            return element.ToString();
        }
    }
}
