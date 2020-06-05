// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnnecessarySuppressionDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpRemoveUnnecessarySuppressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnnecessarySuppressionForIsExpressionDiagnosticId,
                   new LocalizableResourceString(nameof(CSharpFeaturesResources.Remove_unnecessary_suppression_operator), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                   new LocalizableResourceString(nameof(CSharpFeaturesResources.Suppression_operator_has_no_effect_and_can_be_misinterpreted), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)))
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
