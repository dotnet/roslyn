// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal class CSharpAddRequiredParenthesesForCastExpressionDiagnosticAnalyzer :
        AbstractAddRequiredParenthesesDiagnosticAnalyzer<SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> GetSyntaxNodeKinds()
            => ImmutableArray.Create(SyntaxKind.CastExpression);

        protected override void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var preference = optionSet.GetOption(CodeStyleOptions.CastOperationParentheses, LanguageNames.CSharp);
            if (preference.Value != ParenthesesPreference.AlwaysForClarity)
            {
                return;
            }

            var castExpression = (CastExpressionSyntax)context.Node;
            var inner = castExpression.Expression;
            var innerKind = inner.Kind();

            if (!CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer.IsAmbiguousInnerExpressionKindForCast(innerKind))
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(castExpression.Expression.GetLocation());

            context.ReportDiagnostic(Diagnostic.Create(
                GetDescriptorWithSeverity(preference.Notification.Value),
                castExpression.Expression.GetLocation(), additionalLocations));
        }
    }
}
