// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
                   message,
                   CodeStyleOptions2.PreferConditionalExpressionOverReturn)
        {
        }

        protected override bool TryMatchPattern(IConditionalOperation ifOperation, ISymbol containingSymbol)
            => UseConditionalExpressionForReturnHelpers.TryMatchPattern(
                    GetSyntaxFacts(), ifOperation, containingSymbol, out _, out _, out _, out _);
    }
}
