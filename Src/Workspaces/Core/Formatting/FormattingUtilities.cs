using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    internal static class FormattingUtilities
    {
        public static bool HasAnyWhitespaceElasticTrivia(CommonSyntaxToken previousToken, CommonSyntaxToken currentToken)
        {
            if ((!previousToken.ContainsAnnotations && !currentToken.ContainsAnnotations) ||
                (!previousToken.HasTrailingTrivia && !currentToken.HasLeadingTrivia))
            {
                return false;
            }

            return previousToken.TrailingTrivia.HasAnyWhitespaceElasticTrivia() || currentToken.LeadingTrivia.HasAnyWhitespaceElasticTrivia();
        }
    }
}