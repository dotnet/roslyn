// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
{
    internal abstract class AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<
        TLanguageKindEnum,
        TParenthesizedExpressionSyntax>
        : AbstractParenthesesDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TParenthesizedExpressionSyntax : SyntaxNode
    {
        protected AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Remove_unnecessary_parentheses), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Parentheses_can_be_removed), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public sealed override bool OpenFileOnly(Workspace workspace)
            => false;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxNodeKind());

        protected abstract TLanguageKindEnum GetSyntaxNodeKind();
        protected abstract bool CanRemoveParentheses(
            TParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel,
            out PrecedenceKind precedence, out bool clarifiesPrecedence);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationTokan = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationTokan).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var parenthesizedExpression = (TParenthesizedExpressionSyntax)context.Node;

            if (!CanRemoveParentheses(parenthesizedExpression, context.SemanticModel,
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
                    var syntaxFacts = this.GetSyntaxFactsService();
                    var child = syntaxFacts.GetExpressionOfParenthesizedExpression(parenthesizedExpression);

                    var parentKind = parenthesizedExpression.Parent.RawKind;
                    var childKind = child.RawKind;
                    if (parentKind != childKind)
                    {
                        return;
                    }

                    // Ok to remove if it was the exact same kind.  i.e. ```(a | b) | c```
                    // not ok to remove if kinds changed.  i.e. ```(a + b) << c```
                    break;
            }

            var option = GetLanguageOption(precedence);
            var preference = optionSet.GetOption(option, parenthesizedExpression.Language);

            if (preference.Notification.Severity == ReportDiagnostic.Suppress)
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

            var severity = preference.Notification.Severity;

            var additionalLocations = ImmutableArray.Create(parenthesizedExpression.GetLocation());

            context.ReportDiagnostic(DiagnosticHelper.Create(
                UnnecessaryWithSuggestionDescriptor,
                parenthesizedExpression.GetFirstToken().GetLocation(),
                severity,
                additionalLocations,
                properties: null));

            context.ReportDiagnostic(Diagnostic.Create(
                UnnecessaryWithoutSuggestionDescriptor,
                parenthesizedExpression.GetLastToken().GetLocation(), additionalLocations));
        }
    }
}
