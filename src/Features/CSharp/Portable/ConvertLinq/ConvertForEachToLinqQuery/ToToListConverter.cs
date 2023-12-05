// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    /// <summary>
    /// Provides a conversion to query.ToList().
    /// </summary>
    internal sealed class ToToListConverter(
        ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
        ExpressionSyntax selectExpression,
        ExpressionSyntax modifyingExpression,
        SyntaxTrivia[] trivia) : AbstractToMethodConverter(forEachInfo, selectExpression, modifyingExpression, trivia)
    {
        protected override string MethodName => nameof(Enumerable.ToList);

        /// Checks that the expression is "new List();"
        /// Exclude "new List(a);" and new List() { 1, 2, 3}
        protected override bool CanReplaceInitialization(
            ExpressionSyntax expression, CancellationToken cancellationToken)
            => expression is ObjectCreationExpressionSyntax objectCreationExpression &&
               ForEachInfo.SemanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken).Symbol is ITypeSymbol typeSymbol &&
               CSharpConvertForEachToLinqQueryProvider.TypeSymbolIsList(typeSymbol, ForEachInfo.SemanticModel) &&
               (objectCreationExpression.ArgumentList == null || !objectCreationExpression.ArgumentList.Arguments.Any()) &&
               (objectCreationExpression.Initializer == null || !objectCreationExpression.Initializer.Expressions.Any());

        /// Input:
        /// foreach(...)
        /// {
        ///     ...
        ///     ...
        ///     list.Add(item);
        ///  }
        ///  
        ///  Output:
        ///  list.AddRange(queryGenerated);
        protected override StatementSyntax CreateDefaultStatement(ExpressionSyntax queryOrLinqInvocationExpression, ExpressionSyntax expression)
            => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        expression,
                        SyntaxFactory.IdentifierName(nameof(List<object>.AddRange))),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(queryOrLinqInvocationExpression)))));
    }
}
