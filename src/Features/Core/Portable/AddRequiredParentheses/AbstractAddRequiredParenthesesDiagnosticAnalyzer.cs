// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses
{
    internal abstract class AbstractAddRequiredParenthesesDiagnosticAnalyzer<TLanguageKindEnum>
        : AbstractParenthesesDiagnosticAnalyzer
        where TLanguageKindEnum : struct
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
        protected abstract int GetPrecedence(SyntaxNode binaryLike);
        protected abstract PrecedenceKind GetPrecedenceKind(SyntaxNode binaryLike);
        protected abstract SyntaxNode GetParentExpressionOrAssignment(SyntaxNode binaryLike);
        protected abstract bool IsBinaryLike(SyntaxNode node);
        protected abstract void GetPartsOfBinaryLike(
            SyntaxNode binaryLike, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var binaryLike = context.Node;
            var parent = GetParentExpressionOrAssignment(binaryLike);
            if (!IsBinaryLike(parent))
            {
                return;
            }

            var parentBinaryLike = parent;
            if (GetPrecedence(binaryLike) == GetPrecedence(parentBinaryLike))
            {
                return;
            }

            var precedenceKind = GetPrecedenceKind(parentBinaryLike);

            var preference = optionSet.GetOption(GetLanguageOption(precedenceKind), binaryLike.Language);
            if (preference.Value != ParenthesesPreference.RequireForPrecedenceClarity)
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(binaryLike.GetLocation());
            var precedence = GetPrecedence(binaryLike);

            // In a case like "a + b * c * d", we'll add parens to make "a + (b * c * d)".
            // To make this user experience more pleasant, we will place the diagnostic on
            // both *'s.
            AddDiagnostics(
                context, binaryLike, precedence,
                preference.Notification.Value, additionalLocations);
        }

        private void AddDiagnostics(
            SyntaxNodeAnalysisContext context, SyntaxNode binaryLikeOpt,
            int precedence, DiagnosticSeverity severity, ImmutableArray<Location> additionalLocations)
        {
            if (binaryLikeOpt != null && 
                IsBinaryLike(binaryLikeOpt) &&
                GetPrecedence(binaryLikeOpt) == precedence)
            {
                GetPartsOfBinaryLike(
                    binaryLikeOpt, out var left, out var operatorToken, out var right);

                context.ReportDiagnostic(
                    Diagnostic.Create(GetDescriptorWithSeverity(severity), operatorToken.GetLocation(), additionalLocations));

                AddDiagnostics(context, left, precedence, severity, additionalLocations);
                AddDiagnostics(context, right, precedence, severity, additionalLocations);
            }
        }

    }
}
