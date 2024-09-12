// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery;

internal sealed class YieldReturnConverter(
    ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
    YieldStatementSyntax yieldReturnStatement,
    YieldStatementSyntax yieldBreakStatement) : AbstractConverter(forEachInfo)
{
    private readonly YieldStatementSyntax _yieldReturnStatement = yieldReturnStatement;
    private readonly YieldStatementSyntax _yieldBreakStatement = yieldBreakStatement;

    public override void Convert(SyntaxEditor editor, bool convertToQuery, CancellationToken cancellationToken)
    {
        var queryOrLinqInvocationExpression = CreateQueryExpressionOrLinqInvocation(
           selectExpression: _yieldReturnStatement.Expression,
           leadingTokensForSelect: [_yieldReturnStatement.YieldKeyword, _yieldReturnStatement.ReturnOrBreakKeyword],
           trailingTokensForSelect: _yieldBreakStatement != null
                                    ? [_yieldReturnStatement.SemicolonToken,
                                        _yieldBreakStatement.YieldKeyword,
                                        _yieldBreakStatement.ReturnOrBreakKeyword,
                                        _yieldBreakStatement.SemicolonToken]
                                    : [_yieldReturnStatement.SemicolonToken],
           convertToQuery: convertToQuery);

        editor.ReplaceNode(
            ForEachInfo.ForEachStatement,
            SyntaxFactory.ReturnStatement(queryOrLinqInvocationExpression).WithAdditionalAnnotations(Formatter.Annotation));

        // Delete the yield break just after the loop.
        if (_yieldBreakStatement != null)
        {
            editor.RemoveNode(_yieldBreakStatement);
        }
    }
}
