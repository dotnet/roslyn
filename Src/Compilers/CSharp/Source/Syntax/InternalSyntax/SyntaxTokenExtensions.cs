using System.Linq;

namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    internal static class SyntaxTokenExtensions
    {
        public static SyntaxToken AddNodesAsTrailingTrivia(this SyntaxToken token, params SyntaxNode[] nodes)
        {
            var trivia = nodes.ConvertToTriviaList();

            return token.WithTrailingTrivia(SyntaxList.Concat(token.GetTrailingTrivia(), trivia));
        }

        public static SyntaxToken AddNodesAsLeadingTrivia(this SyntaxToken token, params SyntaxNode[] nodes)
        {
            var trivia = nodes.ConvertToTriviaList();

            return token.WithLeadingTrivia(SyntaxList.Concat(trivia, token.GetLeadingTrivia()));
        }
    }
}