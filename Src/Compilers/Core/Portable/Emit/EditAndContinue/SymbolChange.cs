using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal enum SymbolChange
    {
        /// <summary>
        /// No change to symbol or members.
        /// </summary>
        None = 0,

        /// <summary>
        /// No change to symbol but may contain changed symbols.
        /// </summary>
        ContainsChanges,

        /// <summary>
        /// Symbol updated.
        /// </summary>
        Updated,

        /// <summary>
        /// Symbol added.
        /// </summary>
        Added,
    }
}
