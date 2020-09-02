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
            var rightOperand = operation.RightOperand;
            var leftOperand = operation.LeftOperand;
            // The reported additionalLocations represents the non-true side of equality.
            // This is used by the codefix as the location of the replacement node.
            // So, the code fix will do: Replace(diagnostic.Location, diagnostic.AdditionalLocations[0]).
            if (rightOperand.ConstantValue.HasValue && rightOperand.ConstantValue.Value is true)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor,
                    operation.Syntax.GetLocation(),
                    additionalLocations: new[] { leftOperand.Syntax.GetLocation() }));
            }
            else if (leftOperand.ConstantValue.HasValue && leftOperand.ConstantValue.Value is true)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor,
                    operation.Syntax.GetLocation(),
                    additionalLocations: new[] { rightOperand.Syntax.GetLocation() }));
            }
        }
    }
}
