using System;

namespace Analyzer.Utilities.Extensions
{
    internal enum SymbolVisibility
    {
        Public = 0,
        Internal = 1,
        Private = 2,
        Friend = Internal,
    }

    /// <summary>
    /// Extensions for <see cref="SymbolVisibility"/>.
    /// </summary>
    internal static class SymbolVisibilityExtensions
    {
        /// <summary>
        /// Determines whether <paramref name="typeVisibility"/> is at least as visible as <paramref name="comparisonVisibility"/>.
        /// </summary>
        /// <param name="typeVisibility">The visibility to compare against.</param>
        /// <param name="comparisonVisibility">The visibility to compare with.</param>
        /// <returns>True if one can say that <paramref name="typeVisibility"/> is at least as visible as <paramref name="comparisonVisibility"/>.</returns>
        /// <remarks>
        /// For example, <see cref="SymbolVisibility.Public"/> is at least as visible as <see cref="SymbolVisibility.Internal"/>, but <see cref="SymbolVisibility.Private"/> is not as visible as <see cref="SymbolVisibility.Public"/>.
        /// </remarks>
        public static bool IsAtLeastAsVisibleAs(this SymbolVisibility typeVisibility, SymbolVisibility comparisonVisibility)
        {
            return typeVisibility switch
            {
                SymbolVisibility.Public => true,
                SymbolVisibility.Internal => comparisonVisibility != SymbolVisibility.Public,
                SymbolVisibility.Private => comparisonVisibility == SymbolVisibility.Private,
                _ => throw new ArgumentOutOfRangeException(nameof(typeVisibility), typeVisibility, null),
            };
        }

        public static SymbolVisibilityGroup ToSymbolVisibilityGroup(this SymbolVisibility symbolVisibility)
        {
            return symbolVisibility switch
            {
                SymbolVisibility.Public => SymbolVisibilityGroup.Public,

                SymbolVisibility.Internal => SymbolVisibilityGroup.Internal,

                SymbolVisibility.Private => SymbolVisibilityGroup.Private,

                _ => throw new ArgumentOutOfRangeException(nameof(symbolVisibility), symbolVisibility, null),
            };
        }
    }
}