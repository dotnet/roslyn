// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class WhenKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public WhenKeywordRecommender()
        : base(SyntaxKind.WhenKeyword, isValidInPreprocessorContext: true)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsCatchFilterContext ||
            IsAfterCompleteExpressionOrPatternInCaseLabel(context) ||
            IsAtEndOfPatternInSwitchExpression(context);
    }

    private static bool IsAtEndOfPatternInSwitchExpression(CSharpSyntaxContext context)
    {
        if (!context.IsAtEndOfPattern)
            return false;

        // `x switch { SomePattern $$
        var pattern = context.TargetToken.GetAncestors<PatternSyntax>().LastOrDefault();
        if (pattern?.Parent is SwitchExpressionArmSyntax)
            return true;

        return false;
    }

    private static bool IsAfterCompleteExpressionOrPatternInCaseLabel(CSharpSyntaxContext context)
    {
        var switchLabel = context.TargetToken.GetAncestor<SwitchLabelSyntax>();
        if (switchLabel == null)
            return false;

        var expressionOrPattern = switchLabel.ChildNodes().FirstOrDefault();
        if (expressionOrPattern == null)
        {
            // It must have been a default label.
            return false;
        }

        // Never show `when` after `var` in a pattern.  It's virtually always going to be unhelpful as the user is
        // far more likely to be writing `case var someName...` rather than typing `cae var when...` (in the case
        // that `var` is a constant).  In other words, it's fine to make that rare case have to manually type out
        // `when` rather than risk having `when` pop up when it's not desired.
        if (context.TargetToken.Text == "var")
            return false;

        // If the last token is missing, the expression is incomplete - possibly because of missing parentheses,
        // but not necessarily. We don't want to offer 'when' in those cases. Here are some examples that illustrate this:
        // case |
        // case x.|
        // case 1 + |
        // case (1 + 1 |

        // Also note that if there's a missing token inside the expression, that's fine and we do offer 'when':
        // case (1 + ) |

        if (expressionOrPattern.GetLastToken(includeZeroWidth: true).IsMissing)
            return false;

        var lastToken = expressionOrPattern.GetLastToken(includeZeroWidth: false);

        // We're writing past the end of a complete pattern.  This is a place where 'when' could be added to add
        // restrictions on the pattern.
        if (lastToken == context.TargetToken)
            return true;

        // We're writing the last token of a pattern.  In this case, we might either be writing a name for the pattern
        // (like `case Wolf w:`) or we might be starting to write `when` (like `case Wolf when ...:`).
        if (lastToken == context.LeftToken)
        {
            if (expressionOrPattern is DeclarationPatternSyntax)
                return true;

            if (expressionOrPattern is RecursivePatternSyntax)
                return true;
        }

        return false;
    }
}
