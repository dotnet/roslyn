using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    // Wrapper types to ensure we delay load the nuget libraries.
    internal interface IPackageSourceProviderProxy
    {
        event EventHandler SourcesChanged;
        IEnumerable<KeyValuePair<string, string>> GetSources(bool includeUnOfficial, bool includeDisabled);
    }
}
