// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                   CodeStyleOptions.PreferConditionalExpressionOverReturn)
        {
        }

        protected override bool TryMatchPattern(IConditionalOperation ifOperation)
            => UseConditionalExpressionForReturnHelpers.TryMatchPattern(
                    GetSyntaxFactsService(), ifOperation, out _, out _);
    }
}
