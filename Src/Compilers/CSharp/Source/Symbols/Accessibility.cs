using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents the accessibility of a symbol. Note: This enum is ordered by effective
    /// accessibility.
    /// </summary>
    public enum Accessibility
    {
        /// <summary>
        /// Indicates that accessibility is not applicable to this kind of symbol.
        /// </summary>
        NotApplicable,

        Private,

        /// <summary>
        /// Not supported in C#, but should be supported for symbols imported from reference
        /// assemblies in other languages.
        /// </summary>
        ProtectedAndInternal,
        Protected,
        Internal,
        ProtectedInternal,
        Public
    }
}