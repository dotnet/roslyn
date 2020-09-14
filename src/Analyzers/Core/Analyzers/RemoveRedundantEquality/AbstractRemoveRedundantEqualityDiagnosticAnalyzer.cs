// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.RemoveRedundantEquality
{
    internal abstract class AbstractRemoveRedundantEqualityDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private readonly ISyntaxFacts _syntaxFacts;

        protected AbstractRemoveRedundantEqualityDiagnosticAnalyzer(ISyntaxFacts syntaxFacts)
            : base(IDEDiagnosticIds.RemoveRedundantEqualityDiagnosticId,
                   option: null,
                   new LocalizableResourceString(nameof(AnalyzersResources.Remove_redundant_equality), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
            _syntaxFacts = syntaxFacts;
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

            if (!_syntaxFacts.IsBinaryExpression(operation.Syntax))
            {
                return;
            }

            var rightOperand = operation.RightOperand;
            var leftOperand = operation.LeftOperand;
            if (rightOperand.Type.SpecialType is not SpecialType.System_Boolean ||
                leftOperand.Type.SpecialType is not SpecialType.System_Boolean)
            {
                return;
            }

            var isOperatorEquals = operation.OperatorKind == BinaryOperatorKind.Equals;
            _syntaxFacts.GetPartsOfBinaryExpression(operation.Syntax, out _, out var operatorToken, out _);
            // Example: if (x == true) {}
            //     The primary location will be on the operator.
            //     additionalLocations[0] will be on the whole expression "x == true".
            //     additionalLocations[1] will be on the non-constant side "x"
            // additionalLocations is used by the codefix as follows:
            //     Replace(node at additionalLocations[0], node at AdditionalLocations[1]). i.e. Replace(`x == true`, `x`)
            if (TryGetLiteralValue(rightOperand, out var result) && result == isOperatorEquals)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor,
                    operatorToken.GetLocation(),
                    additionalLocations: new[] { operation.Syntax.GetLocation(), leftOperand.Syntax.GetLocation() }));
            }
            else if (TryGetLiteralValue(leftOperand, out result) && result == isOperatorEquals)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor,
                    operatorToken.GetLocation(),
                    additionalLocations: new[] { operation.Syntax.GetLocation(), rightOperand.Syntax.GetLocation() }));
            }

            return;

            static bool TryGetLiteralValue(IOperation operand, [NotNullWhen(true)] out bool? result)
            {
                if (operand.ConstantValue.HasValue && operand.Kind == OperationKind.Literal)
                {
                    if (operand.ConstantValue.Value is true)
                    {
                        result = true;
                        return true;
                    }
                    if (operand.ConstantValue.Value is false)
                    {
                        result = false;
                        return true;
                    }
                }

                result = null;
                return false;
            }
        }
    }
}
