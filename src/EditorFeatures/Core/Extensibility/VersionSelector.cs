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
        public static T Select<T>(IEnumerable<Lazy<T, VisualStudioVersionMetadata>> items)
        {
            var best = items.FirstOrDefault();
            foreach (var item in items.Skip(1))
            {
                if ((int)item.Metadata.Version > (int)best.Metadata.Version)
                {
                    best = item;
                }
            }

            return best.Value;
        }
    }
}
