// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer<
        TIfStatementSyntax>
        : AbstractUseConditionalExpressionDiagnosticAnalyzer<TIfStatementSyntax>
        where TIfStatementSyntax : SyntaxNode
    {
        protected AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer(
            LocalizableResourceString message)
            : base(IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId,
                   EnforceOnBuildValues.UseConditionalExpressionForReturn,
                   message,
                   CodeStyleOptions2.PreferConditionalExpressionOverReturn)
        {
        }

        protected sealed override CodeStyleOption2<bool> GetStylePreference(OperationAnalysisContext context)
            => context.GetAnalyzerOptions().PreferConditionalExpressionOverReturn;

        protected override (bool matched, bool canSimplify) TryMatchPattern(IConditionalOperation ifOperation, ISymbol containingSymbol)
        {
            if (!UseConditionalExpressionForReturnHelpers.TryMatchPattern(
                    GetSyntaxFacts(), ifOperation, containingSymbol, out var isRef, out var trueStatement, out var falseStatement, out var trueReturn, out var falseReturn))
            {
                return default;
            }

            var canSimplify = UseConditionalExpressionHelpers.CanSimplify(
                trueReturn?.ReturnedValue ?? trueStatement,
                falseReturn?.ReturnedValue ?? falseStatement,
                isRef,
                out _);

            return (matched: true, canSimplify);
        }
    }
}
