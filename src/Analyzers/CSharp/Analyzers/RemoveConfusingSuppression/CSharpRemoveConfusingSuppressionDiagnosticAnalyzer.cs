// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveConfusingSuppressionDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpRemoveConfusingSuppressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveConfusingSuppressionForIsExpressionDiagnosticId,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_unnecessary_suppression_operator), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Suppression_operator_has_no_effect_and_can_be_misinterpreted), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IsExpression, SyntaxKind.IsPatternExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var left = node switch
            {
                BinaryExpressionSyntax binary => binary.Left,
                IsPatternExpressionSyntax isPattern => isPattern.Expression,
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };

            if (left.Kind() != SyntaxKind.SuppressNullableWarningExpression)
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                ((PostfixUnaryExpressionSyntax)left).OperatorToken.GetLocation(),
                ReportDiagnostic.Warn,
                ImmutableArray.Create(node.GetLocation()),
                properties: null));
        }
    }
}
