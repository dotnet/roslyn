using System.Linq;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToInterpolatedString
{
    internal partial class ConvertToInterpolatedStringRefactoringProvider
    {
        private class MultiLineCommentInInterpolatedStringFormattingRule : AbstractFormattingRule
        {
            private bool ForceSingleSpace(SyntaxToken previousToken, SyntaxToken currentToken)
            {
                return currentToken.GetAllTrivia().Any(t => t.IsKind(SyntaxKind.MultiLineCommentTrivia));
            }

            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
            {
                if (ForceSingleSpace(previousToken, currentToken))
                {
                    return null;
                }

                return base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, nextOperation);
            }

            public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
            {
                if (ForceSingleSpace(previousToken, currentToken))
                {
                    return new AdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }

                return base.GetAdjustSpacesOperation(previousToken, currentToken, optionSet, nextOperation);
            }
        }
    }
}
