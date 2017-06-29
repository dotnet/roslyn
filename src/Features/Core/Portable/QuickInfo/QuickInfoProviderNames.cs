using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Some of the known <see cref="QuickInfoProvider"/> names in use.
    /// Names are used for ordering providers with the <see cref="ExtensionOrderAttribute"/>.
    /// </summary>
    internal static class QuickInfoProviderNames
    {
        public const string Semantic = nameof(Semantic);
        public const string Syntactic = nameof(Syntactic);
    }
}