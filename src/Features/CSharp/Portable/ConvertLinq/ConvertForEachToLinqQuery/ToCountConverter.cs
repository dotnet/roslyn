// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    /// <summary>
    /// Provides a conversion to query.Count().
    /// </summary>
    internal sealed class ToCountConverter : AbstractToMethodConverter
    {
        public ToCountConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            ExpressionSyntax selectExpression,
            ExpressionSyntax modifyingExpression,
            SyntaxTrivia[] trivia)
            : base(forEachInfo, selectExpression, modifyingExpression, trivia)
        {
        }

        protected override string MethodName => nameof(Enumerable.Count);

        // Checks that the expression is "0".
        protected override bool CanReplaceInitialization(
            ExpressionSyntax expression,
            CancellationToken cancellationToken)
            => expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.ValueText == "0";

        /// Input:
        /// foreach(...)
        /// {
        ///     ...
        ///     ...
        ///     counter++;
        ///  }
        ///  
        ///  Output:
        ///  counter += queryGenerated.Count();
        protected override StatementSyntax CreateDefaultStatement(ExpressionSyntax queryOrLinqInvocationExpression, ExpressionSyntax expression)
            => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    expression,
                    CreateInvocationExpression(queryOrLinqInvocationExpression)));
    }
}
