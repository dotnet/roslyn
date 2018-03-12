// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

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

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public sealed override bool OpenFileOnly(Workspace workspace)
            => false;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxNodeKind());

        protected abstract TLanguageKindEnum GetSyntaxNodeKind();
        protected abstract bool CanRemoveParentheses(
            TParenthesizedExpressionSyntax parenthesizedExpression, SemanticModel semanticModel,
            out PrecedenceKind precedenceKind, out bool clarifiesPrecedence);

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
                    out var precedenceKind, out bool clarifiesPrecedence))
            {
                return;
            }

            var option = GetLanguageOption(precedenceKind);
            var preference = optionSet.GetOption(option, parenthesizedExpression.Language);

            if (preference.Value == ParenthesesPreference.Ignore)
            {
                // User doesn't care about these parens.  So nothing for us to do.
                return;
            }

            if (preference.Value == ParenthesesPreference.RequireForPrecedenceClarity &&
                clarifiesPrecedence)
            {
                // User wants these parens if they calrify precedence, and these parens
                // clarify precedence.  So keep these around.
                return;
            }

            var severity = preference.Notification.Value;

            var additionalLocations = ImmutableArray.Create(parenthesizedExpression.GetLocation());

            context.ReportDiagnostic(Diagnostic.Create(
                CreateUnnecessaryDescriptor(severity),
                parenthesizedExpression.GetFirstToken().GetLocation(), additionalLocations));

            context.ReportDiagnostic(Diagnostic.Create(
                CreateUnnecessaryDescriptor(DiagnosticSeverity.Hidden),
                parenthesizedExpression.GetLastToken().GetLocation(), additionalLocations));
        }

        protected static bool TypeIsBoolean(SyntaxNode node, SemanticModel semanticModel)
        {
            var type = semanticModel.GetTypeInfo(node).Type;
            return type?.SpecialType == SpecialType.System_Boolean;
        }
    }
}
