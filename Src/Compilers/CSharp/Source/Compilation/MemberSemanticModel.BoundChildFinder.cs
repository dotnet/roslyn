
namespace Roslyn.Compilers.CSharp
{
    partial class MemberSemanticModel
    {
        class BoundChildFinder : BoundTreeWalker
        {
            private readonly SyntaxNode syntax;
            private BoundNode found;

            private BoundChildFinder(SyntaxNode syntax)
            {
                this.syntax = syntax;
            }

            public static BoundNode FindChildForSyntax(BoundNode parent, SyntaxNode childSyntax)
            {
                var finder = new BoundChildFinder(childSyntax);
                finder.Visit(parent);
                return finder.found;
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (found == null)
                {
                    if (node != null && !node.WasCompilerGenerated && node.Syntax == syntax)
                    {
                        found = node;
                    }
                    else
                    {
                        base.Visit(node);
                    }
                }
                return null;
            }
        }
    }
}