// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.RemoveRedundantEqualityWithTrue
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class RemoveRedundantEqualityWithTrueDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString s_title =
            new(nameof(AnalyzersResources.Remove_redundant_equality_with_true), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        public RemoveRedundantEqualityWithTrueDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveRedundantEqualityWithTrueDiagnosticId,
                  option: null,
                  s_title)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);

        private void AnalyzeBinaryOperator(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind != BinaryOperatorKind.Equals)
            {
                return;
            }
            var rightValue = operation.RightOperand.ConstantValue;
            var leftValue = operation.LeftOperand.ConstantValue;
            if ((rightValue.HasValue && rightValue.Value is true) ||
                (leftValue.HasValue && leftValue.Value is true))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, operation.Syntax.GetLocation()));
            }
        }
    }
}
