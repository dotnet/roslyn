using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        private class RemoveNodeRewriter : CSharpSyntaxRewriter
        {
            private readonly SyntaxNode arg;
            internal RemoveNodeRewriter(SyntaxNode arg)
            {
                this.arg = arg;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == arg)
                {
                    return null;
                }
                else
                {
                    return base.Visit(node);
                }
            }
        }
    }
}
