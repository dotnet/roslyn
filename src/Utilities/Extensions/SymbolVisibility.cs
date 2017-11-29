using System;

namespace Analyzer.Utilities.Extensions
{
    internal enum SymbolVisibility
    {
        Public,
        Internal,
        Private,
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
            switch (typeVisibility)
            {
                case SymbolVisibility.Public:
                    return true;
                case SymbolVisibility.Internal:
                    return comparisonVisibility != SymbolVisibility.Public;
                case SymbolVisibility.Private:
                    return comparisonVisibility == SymbolVisibility.Private;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeVisibility), typeVisibility, null);
            }
        }
    }
}