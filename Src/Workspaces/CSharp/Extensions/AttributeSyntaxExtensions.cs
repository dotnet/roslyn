using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class AttributeSyntaxExtensions
    {
        public static AttributeSyntax MakeSemanticallyExplicit(
            this AttributeSyntax statement,
            Document document,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return (AttributeSyntax)Simplifier.Expand(document, statement, cancellationToken);
        }
    }
}
