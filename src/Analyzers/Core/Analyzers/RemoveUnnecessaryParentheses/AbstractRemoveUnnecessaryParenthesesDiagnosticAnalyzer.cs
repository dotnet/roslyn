// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

internal abstract class AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<
    TLanguageKindEnum,
    TParenthesizedExpressionSyntax>
    : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
    where TLanguageKindEnum : struct
    where TParenthesizedExpressionSyntax : SyntaxNode
{
    protected AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId,
              EnforceOnBuildValues.RemoveUnnecessaryParentheses,
              options: ParenthesesDiagnosticAnalyzersHelper.Options,
              fadingOption: null,
              new LocalizableResourceString(nameof(AnalyzersResources.Remove_unnecessary_parentheses), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
              new LocalizableResourceString(nameof(AnalyzersResources.Parentheses_can_be_removed), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    protected abstract TLanguageKindEnum GetSyntaxKind();
    protected abstract ISyntaxFacts GetSyntaxFacts();

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxKind());

    protected abstract bool CanRemoveParentheses(
        TParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel, CancellationToken cancellationToken,
        out PrecedenceKind precedence, out bool clarifiesPrecedence);

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var parenthesizedExpression = (TParenthesizedExpressionSyntax)context.Node;

        if (!CanRemoveParentheses(parenthesizedExpression, context.SemanticModel, cancellationToken,
                out var precedence, out var clarifiesPrecedence))
        {
            return;
        }

        // Do not remove parentheses from these expressions when there are different kinds
        // between the parent and child of the parenthesized expr..  This is because removing
        // these parens can significantly decrease readability and can confuse many people
        // (including several people quizzed on Roslyn).  For example, most people see
        // "1 + 2 << 3" as "1 + (2 << 3)", when it's actually "(1 + 2) << 3".  To avoid 
        // making code bases more confusing, we just do not touch parens for these constructs 
        // unless both the child and parent have the same kinds.
        switch (precedence)
        {
            case PrecedenceKind.Shift:
            case PrecedenceKind.Bitwise:
            case PrecedenceKind.Coalesce:
                var syntaxFacts = GetSyntaxFacts();
                var child = syntaxFacts.GetExpressionOfParenthesizedExpression(parenthesizedExpression);

                var parentKind = parenthesizedExpression.Parent?.RawKind;
                var childKind = child.RawKind;
                if (parentKind != childKind)
                {
                    return;
                }

                // Ok to remove if it was the exact same kind.  i.e. ```(a | b) | c```
                // not ok to remove if kinds changed.  i.e. ```(a + b) << c```
                break;
        }

        var options = context.GetAnalyzerOptions();
        var preference = ParenthesesDiagnosticAnalyzersHelper.GetLanguageOption(options, precedence);

        if (ShouldSkipAnalysis(context, preference.Notification))
        {
            // User doesn't care about these parens.  So nothing for us to do.
            return;
        }

        if (preference.Value == ParenthesesPreference.AlwaysForClarity &&
            clarifiesPrecedence)
        {
            // User wants these parens if they clarify precedence, and these parens
            // clarify precedence.  So keep these around.
            return;
        }

        // either they don't want unnecessary parentheses, or they want them only for
        // clarification purposes and this does not make things clear.
        Debug.Assert(preference.Value == ParenthesesPreference.NeverIfUnnecessary ||
                     !clarifiesPrecedence);

        var additionalLocations = ImmutableArray.Create(
            parenthesizedExpression.GetLocation());
        var additionalUnnecessaryLocations = ImmutableArray.Create(
            parenthesizedExpression.GetFirstToken().GetLocation(),
            parenthesizedExpression.GetLastToken().GetLocation());

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            Descriptor,
            AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<TLanguageKindEnum, TParenthesizedExpressionSyntax>.GetDiagnosticSquiggleLocation(parenthesizedExpression, cancellationToken),
            preference.Notification,
            context.Options,
            additionalLocations,
            additionalUnnecessaryLocations));
    }

    /// <summary>
    /// Gets the span of text to squiggle underline.
    /// If the expression is contained within a single line, the entire expression span is returned.
    /// Otherwise it will return the span from the expression start to the end of the same line.
    /// </summary>
    private static Location GetDiagnosticSquiggleLocation(TParenthesizedExpressionSyntax parenthesizedExpression, CancellationToken cancellationToken)
    {
        var parenthesizedExpressionLocation = parenthesizedExpression.GetLocation();

        var lines = parenthesizedExpression.SyntaxTree.GetText(cancellationToken).Lines;
        var expressionFirstLine = lines.GetLineFromPosition(parenthesizedExpressionLocation.SourceSpan.Start);

        var textSpanEndPosition = Math.Min(parenthesizedExpressionLocation.SourceSpan.End, expressionFirstLine.Span.End);
        return Location.Create(parenthesizedExpression.SyntaxTree, TextSpan.FromBounds(parenthesizedExpressionLocation.SourceSpan.Start, textSpanEndPosition));
    }
}
