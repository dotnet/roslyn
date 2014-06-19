
namespace Roslyn.Compilers.CSharp
{
    // This class takes a syntax node -- typically a block -- and returns back a map of every syntax
    // token in the block that is the identifier of a local, lambda parameter, or range variable
    // declaration.
    internal sealed class DeclarationFinder : SyntaxWalker
    {
        private readonly MultiDictionary<string, SyntaxToken> map;

        private DeclarationFinder()
            : base(false)
        {
            this.map = new MultiDictionary<string, SyntaxToken>();
        }

        public static MultiDictionary<string, SyntaxToken> GetAllDeclarations(SyntaxNode syntax)
        {
            var finder = new DeclarationFinder();
            finder.Visit(syntax);
            return finder.map;
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            map.Add(node.Identifier.ValueText, node.Identifier);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            if (node.Identifier != null)
            {
                map.Add(node.Identifier.ValueText, node.Identifier);
            }
            base.VisitCatchDeclaration(node);
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            map.Add(node.Identifier.ValueText, node.Identifier);
            base.VisitParameter(node);
        }

        public override void VisitFromClause(FromClauseSyntax node)
        {
            map.Add(node.Identifier.ValueText, node.Identifier);
            base.VisitFromClause(node);
        }

        public override void VisitLetClause(LetClauseSyntax node)
        {
            map.Add(node.Identifier.ValueText, node.Identifier);
            base.VisitLetClause(node);
        }

        public override void VisitJoinClause(JoinClauseSyntax node)
        {
            map.Add(node.Identifier.ValueText, node.Identifier);
            base.VisitJoinClause(node);
        }

        public override void VisitJoinIntoClause(JoinIntoClauseSyntax node)
        {
            map.Add(node.Identifier.ValueText, node.Identifier);
            base.VisitJoinIntoClause(node);
        }

        public override void VisitQueryContinuation(QueryContinuationSyntax node)
        {
            map.Add(node.Identifier.ValueText, node.Identifier);
            base.VisitQueryContinuation(node);
        }
    }
}