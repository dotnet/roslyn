// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses
{
    internal abstract class AbstractAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer<
        TExpressionSyntax, TBinaryLikeExpressionSyntax, TLanguageKindEnum>
        : AbstractAddRequiredParenthesesDiagnosticAnalyzer<TLanguageKindEnum>
        where TExpressionSyntax : SyntaxNode
        where TBinaryLikeExpressionSyntax : TExpressionSyntax
        where TLanguageKindEnum : struct
    {
        protected abstract int GetPrecedence(TBinaryLikeExpressionSyntax binaryLike);
        protected abstract PrecedenceKind GetPrecedenceKind(TBinaryLikeExpressionSyntax binaryLike);
        protected abstract TExpressionSyntax TryGetParentExpression(TBinaryLikeExpressionSyntax binaryLike);
        protected abstract bool IsBinaryLike(TExpressionSyntax node);
        protected abstract void GetPartsOfBinaryLike(
            TBinaryLikeExpressionSyntax binaryLike, out TExpressionSyntax left, out SyntaxToken operatorToken, out TExpressionSyntax right);

        protected override void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var binaryLike = (TBinaryLikeExpressionSyntax)context.Node;
            var parent = TryGetParentExpression(binaryLike);
            if (parent == null || !IsBinaryLike(parent))
            {
                return;
            }

            var parentBinaryLike = (TBinaryLikeExpressionSyntax)parent;
            if (GetPrecedence(binaryLike) == GetPrecedence(parentBinaryLike))
            {
                return;
            }

            var precedenceKind = GetPrecedenceKind(parentBinaryLike);

            var preference = optionSet.GetOption(GetLanguageOption(precedenceKind), binaryLike.Language);
            if (preference.Value != ParenthesesPreference.AlwaysForClarity)
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
            SyntaxNodeAnalysisContext context, TBinaryLikeExpressionSyntax binaryLikeOpt,
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

                AddDiagnostics(context, left as TBinaryLikeExpressionSyntax, precedence, severity, additionalLocations);
                AddDiagnostics(context, right as TBinaryLikeExpressionSyntax, precedence, severity, additionalLocations);
            }
        }
    }
}
