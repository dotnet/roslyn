using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Enumeration of the different source code compatibility modes.
    /// </summary>
    public enum CompatibilityMode
    {
        /// <summary>
        /// No defined compatibility mode.
        /// </summary>
        None = 0,

        /// <summary>
        /// The parser accepts only syntax that is included in the ISO/IEC 23270:2003 C# language specification. This
        /// mode represents the csc switch /langversion:ISO-1
        /// </summary>
        ECMA1 = 1,

        /// <summary>
        /// The compiler accepts only syntax that is included in the ISO/IEC 23270:2006 C# language specification. This
        /// mode represents the csc switch /langversion:ISO-2
        /// </summary>
        ECMA2 = 2 
    }

    internal static partial class CompatibilityModeEnumBounds
    {
        internal static bool IsValid(this CompatibilityMode value)
        {
            return value >= CompatibilityMode.None && value <= CompatibilityMode.ECMA2;
        }
    }
}