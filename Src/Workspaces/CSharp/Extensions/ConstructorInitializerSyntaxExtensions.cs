using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ConstructorInitializerSyntaxExtensions
    {
        public static ConstructorInitializerSyntax MakeSemanticallyExplicit(
            this ConstructorInitializerSyntax statement,
            Document document,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return (ConstructorInitializerSyntax)Simplifier.Expand(document, statement, cancellationToken);
        }
    }
}
