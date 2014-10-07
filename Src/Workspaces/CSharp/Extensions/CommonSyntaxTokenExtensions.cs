using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class CommonSyntaxTokenExtensions
    {
#if REMOVE
        public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return ((SyntaxToken)token).IsKindOrHasMatchingText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return ((SyntaxToken)token).HasMatchingText(kind);
        }

        public static bool IsParentKind(this SyntaxToken token, SyntaxKind kind)
        {
            return ((SyntaxToken)token).IsParentKind(kind);
        }

        public static bool MatchesKind(this SyntaxToken token, SyntaxKind kind)
        {
            return ((SyntaxToken)token).MatchesKind(kind);
        }

        public static bool MatchesKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
        {
            return ((SyntaxToken)token).MatchesKind(kind1, kind2);
        }

        public static bool MatchesKind(this SyntaxToken token, params SyntaxKind[] kinds)
        {
            return ((SyntaxToken)token).MatchesKind(kinds);
        }
#endif
    }
}
