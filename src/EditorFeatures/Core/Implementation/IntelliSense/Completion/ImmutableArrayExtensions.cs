using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal static class ImmutableArrayExtesions
    {
        public static Model GetSelectedModelOrNull(this ImmutableArray<Model> models)
        {
            if (models == default(ImmutableArray<Model>))
            {
                return null;
            }

            return models.FirstOrDefault(m => m.IsSelected);
        }
    }
}
