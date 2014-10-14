using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal interface IMefHostExportProvider
    {
        IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>();
        IEnumerable<Lazy<TExtension>> GetExports<TExtension>();
    }
}
