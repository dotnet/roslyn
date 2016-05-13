using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Extensibility.Composition;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class VersionSelector
    {
        public static T SelectHighest<T>(IEnumerable<Lazy<T, VisualStudioVersionMetadata>> items)
        {
            return items.OrderByDescending(i => i.Metadata.Version).First().Value;
        }
    }
}
