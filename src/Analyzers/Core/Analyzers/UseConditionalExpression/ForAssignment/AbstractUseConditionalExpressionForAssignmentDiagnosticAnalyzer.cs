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
                   message,
                   CodeStyleOptions2.PreferConditionalExpressionOverAssignment)
        {
        }

        protected override bool TryMatchPattern(IConditionalOperation ifOperation, ISymbol containingSymbol)
            => UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(
                GetSyntaxFacts(), ifOperation, out _, out _, out _, out _);
    }
}
