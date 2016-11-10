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

        public static T SelectVersion<T>(
            IEnumerable<Lazy<T, VisualStudioVersionMetadata>> items,
            VisualStudioVersion version)
        {
            return items.First(i => i.Metadata.Version == version).Value;
        }
    }
}
