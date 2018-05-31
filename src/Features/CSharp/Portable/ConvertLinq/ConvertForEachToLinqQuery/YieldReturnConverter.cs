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
        private readonly YieldStatementSyntax _yieldStatement;
        private readonly SyntaxNode _nodeToDelete;

        public YieldReturnConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            YieldStatementSyntax yieldStatement,
            SyntaxNode nodeToDelete) : base(forEachInfo)
        {
            _yieldStatement = yieldStatement;
            _nodeToDelete = nodeToDelete;
        }

        public override void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var queryExpression = CreateQueryExpression(_forEachInfo, _yieldStatement.Expression);
            editor.ReplaceNode(
                _forEachInfo.ForEachStatement,
                SyntaxFactory.ReturnStatement(queryExpression)
                    .WithAdditionalAnnotations(Formatter.Annotation)
                    .AddBeforeLeadingTrivia(_yieldStatement.YieldKeyword, _yieldStatement.SemicolonToken, _nodeToDelete));
            // Delete the yield break just after the loop.
            if (_nodeToDelete != null)
            {
                editor.RemoveNode(_nodeToDelete);
            }
        }
    }
}
