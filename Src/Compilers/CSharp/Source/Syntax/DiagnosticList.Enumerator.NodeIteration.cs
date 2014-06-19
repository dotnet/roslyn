using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial struct DiagnosticList
    {
        partial struct Enumerator
        {
            private struct NodeIteration
            {
                internal Syntax.InternalSyntax.SyntaxNode Node;
                internal int DiagnosticIndex;
                internal int SlotIndex;

                internal NodeIteration(Syntax.InternalSyntax.SyntaxNode node)
                {
                    this.Node = node;
                    this.SlotIndex = -1;
                    this.DiagnosticIndex = -1;
                }
            }
        }
    }
}