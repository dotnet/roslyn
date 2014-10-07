using System.Collections.Generic;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.CSharp.Extensions
{
    internal static partial class ExpressionSyntaxExtensions
    {
        private class LocalReferenceFinder : SyntaxWalker
        {
            private readonly string name;
            private readonly List<IdentifierNameSyntax> references;

            private LocalReferenceFinder(string name)
            {
                this.name = name;
                this.references = new List<IdentifierNameSyntax>();
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.GetText() == name)
                {
                    references.Add(node);
                }
            }

            public static IEnumerable<IdentifierNameSyntax> Search(string name, SyntaxNode scope)
            {
                var finder = new LocalReferenceFinder(name);
                finder.Visit(scope);
                return finder.references;
            }
        }
    }
}
