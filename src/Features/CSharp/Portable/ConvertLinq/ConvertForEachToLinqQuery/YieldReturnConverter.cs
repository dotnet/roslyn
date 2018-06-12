// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal sealed class YieldReturnConverter : AbstractConverter
    {
        private readonly YieldStatementSyntax _yieldReturnStatement;
        private readonly YieldStatementSyntax _yieldBreakStatement;

        public YieldReturnConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            YieldStatementSyntax yieldReturnStatement,
             YieldStatementSyntax yieldBreakStatement) : base(forEachInfo)
        {
            _yieldReturnStatement = yieldReturnStatement;
            _yieldBreakStatement = yieldBreakStatement;
        }

        public override void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            SyntaxToken[] trailingTokens;
            
            if (_yieldBreakStatement != null)
            {
                trailingTokens = new[] {
                    _yieldReturnStatement.SemicolonToken,
                    _yieldBreakStatement.YieldKeyword,
                    _yieldBreakStatement.ReturnOrBreakKeyword,
                    _yieldBreakStatement.SemicolonToken};
            }
            else
            {
                trailingTokens = new[] { _yieldReturnStatement.SemicolonToken };
            }

            var queryExpression = CreateQueryExpression(
               _forEachInfo,
               _yieldReturnStatement.Expression,
               new[] { _yieldReturnStatement.YieldKeyword, _yieldReturnStatement.ReturnOrBreakKeyword },
               trailingTokens);

            editor.ReplaceNode(
                _forEachInfo.ForEachStatement,
                SyntaxFactory.ReturnStatement(queryExpression)
                    .WithAdditionalAnnotations(Formatter.Annotation));
            // Delete the yield break just after the loop.
            if (_yieldBreakStatement != null)
            {
                editor.RemoveNode(_yieldBreakStatement);
            }
        }
    }
}
