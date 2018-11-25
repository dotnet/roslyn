using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Simplification
{
    // When applied to a SyntaxNode, prevents AbstractImportsAdder from adding imports for this
    // node. Applied alongside SymbolAnnotation when a type should be simplified without adding a
    // using for the type.
    //
    // For example, override completion adds void goo() => throw new
    // System.NotImplementedException() with a SymbolAnnotation and a DoNotAddImportsAnnotation.
    //
    // This allows the simplifier to remove the `System.` if `using System` is already in the file
    // but prevents the addition of `using System` just for the NotImplementedException. 
    //
    // This could have been implemented as an additional bit serialized into the `Data` string of
    // SymbolAnnotation. However that would require additional substring operations to retrieve
    // SymbolAnnotation symbols even in the common case where we don't need to suppress add imports.
    // This is therefore implemented as a separate annotation.
    internal class DoNotAddImportsAnnotation
    {
        public static readonly SyntaxAnnotation Annotation = new SyntaxAnnotation(Kind);
        public const string Kind = "DoNotAddImports";
    }
}
