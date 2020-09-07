// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.RemoveRedundantEquality
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class RemoveRedundantEqualityDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public RemoveRedundantEqualityDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveRedundantEqualityDiagnosticId,
                   option: null,
                   new LocalizableResourceString(nameof(AnalyzersResources.Remove_redundant_equality), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);

        private void AnalyzeBinaryOperator(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorMethod is not null)
            {
                // We shouldn't report diagnostic on overloaded operator as the behavior can change.
                return;
            }

            if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
            {
                return;
            }

            var rightOperand = operation.RightOperand;
            var leftOperand = operation.LeftOperand;
            // The reported additionalLocations represents the non-redundant side of equality.
            // For example: if (x == true) {}
            //     additionalLocations: x
            //     primary Location:    x == true
            // additionalLocations is used by the codefix as the location of the replacement node.
            // So, the code fix will do: Replace(node at diagnostic.Location, node at diagnostic.AdditionalLocations[0]).
            if (rightOperand.ConstantValue.HasValue && rightOperand.ConstantValue.Value is true)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor,
                    rightOperand.Syntax.GetLocation(),
                    ReportDiagnostic.Hidden,
                    additionalLocations: new[] { leftOperand.Syntax.GetLocation() },
                    properties: null));
            }
            else if (leftOperand.ConstantValue.HasValue && leftOperand.ConstantValue.Value is true)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor,
                    leftOperand.Syntax.GetLocation(),
                    ReportDiagnostic.Hidden,
                    additionalLocations: new[] { rightOperand.Syntax.GetLocation() },
                    properties: null));
            }
        }
    }
}
