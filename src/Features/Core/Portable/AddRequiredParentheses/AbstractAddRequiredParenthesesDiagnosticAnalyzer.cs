// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses
{
    internal abstract class AbstractAddRequiredParenthesesDiagnosticAnalyzer<
        TLanguageKindEnum,
        TBinaryExpressionSyntax>
        : AbstractParenthesesDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TBinaryExpressionSyntax : SyntaxNode
    {
        protected AbstractAddRequiredParenthesesDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Add_parentheses_for_clarity), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Parentheses_should_be_added_for_clarity), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public sealed override bool OpenFileOnly(Workspace workspace)
            => false;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxNodeKinds());

        protected abstract ImmutableArray<TLanguageKindEnum> GetSyntaxNodeKinds();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract int GetPrecedence(TBinaryExpressionSyntax binaryExpression);
        protected abstract PrecedenceKind GetPrecedenceKind(TBinaryExpressionSyntax binaryExpression, SemanticModel semanticModel);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationTokan = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationTokan).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var binaryExpression = (TBinaryExpressionSyntax)context.Node;
            if (!(binaryExpression.Parent is TBinaryExpressionSyntax parentBinary))
            {
                return;
            }

            if (GetPrecedence(binaryExpression) == GetPrecedence(parentBinary))
            {
                return;
            }

            var precedenceKind = GetPrecedenceKind(parentBinary, context.SemanticModel);

            var preference = optionSet.GetOption(GetLanguageOption(precedenceKind), binaryExpression.Language);
            if (preference.Value != ParenthesesPreference.RequireForPrecedenceClarity)
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(binaryExpression.GetLocation());
            var precedence = GetPrecedence(binaryExpression);

            // In a case like "a + b * c * d", we'll add parens to make "a + (b * c * d)".
            // To make this user experience more pleasant, we will place the diagnostic on
            // both *'s.
            AddDiagnostics(
                context, binaryExpression, precedence,
                preference.Notification.Value, additionalLocations);
        }

        private void AddDiagnostics(
            SyntaxNodeAnalysisContext context, TBinaryExpressionSyntax binaryExpressionOpt,
            int precedence, DiagnosticSeverity severity, ImmutableArray<Location> additionalLocations)
        {
            if (binaryExpressionOpt != null && GetPrecedence(binaryExpressionOpt) == precedence)
            {
                var syntaxFacts = GetSyntaxFactsService();
                syntaxFacts.GetPartsOfBinaryExpression(
                    binaryExpressionOpt, out var left, out var operatorToken, out var right);

                context.ReportDiagnostic(
                    Diagnostic.Create(GetDescriptorWithSeverity(severity), operatorToken.GetLocation(), additionalLocations));

                AddDiagnostics(context, left as TBinaryExpressionSyntax, precedence, severity, additionalLocations);
                AddDiagnostics(context, right as TBinaryExpressionSyntax, precedence, severity, additionalLocations);
            }
        }
    }
}
