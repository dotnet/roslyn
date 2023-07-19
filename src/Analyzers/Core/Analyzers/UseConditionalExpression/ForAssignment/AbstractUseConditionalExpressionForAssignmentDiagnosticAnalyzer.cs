// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<
        TIfStatementSyntax>
        : AbstractUseConditionalExpressionDiagnosticAnalyzer<TIfStatementSyntax>
        where TIfStatementSyntax : SyntaxNode
    {
        protected AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer(
            LocalizableResourceString message)
            : base(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId,
                   EnforceOnBuildValues.UseConditionalExpressionForAssignment,
                   message,
                   CodeStyleOptions2.PreferConditionalExpressionOverAssignment)
        {
        }

        protected sealed override CodeStyleOption2<bool> GetStylePreference(OperationAnalysisContext context)
            => context.GetAnalyzerOptions().PreferConditionalExpressionOverAssignment;

        protected override (bool matched, bool canSimplify) TryMatchPattern(IConditionalOperation ifOperation, ISymbol containingSymbol)
        {
            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(
                    GetSyntaxFacts(), ifOperation, out var isRef, out var trueStatement, out var falseStatement, out var trueAssignment, out var falseAssignment))
            {
                return default;
            }

            var canSimplify = UseConditionalExpressionHelpers.CanSimplify(
                trueAssignment?.Value ?? trueStatement,
                falseAssignment?.Value ?? falseStatement,
                isRef,
                out _);

            return (matched: true, canSimplify);
        }
    }
}
