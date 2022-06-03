// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class LambdaSyntaxFacts
    {
        public abstract SyntaxNode GetLambda(SyntaxNode lambdaOrLambdaBodySyntax);

        /// <summary>
        /// When invoked on a node that represents an anonymous function or a query clause [1]
        /// with a <paramref name="lambdaOrLambdaBodySyntax"/> of another anonymous function or a query clause of the same kind [2], 
        /// returns the body of the [1] that positionally corresponds to the specified <paramref name="lambdaOrLambdaBodySyntax"/>.
        /// 
        /// E.g. join clause declares left expression and right expression -- each of these expressions is a lambda body.
        /// JoinClause1.GetCorrespondingLambdaBody(JoinClause2.RightExpression) returns JoinClause1.RightExpression.
        /// </summary>
        public abstract SyntaxNode? TryGetCorrespondingLambdaBody(SyntaxNode previousLambdaSyntax, SyntaxNode lambdaOrLambdaBodySyntax);

        /// <summary>
        /// Given a node that represents a variable declaration, lambda or a closure scope return the position to be used to calculate 
        /// the node's syntax offset with respect to its containing member.
        /// </summary>
        public abstract int GetDeclaratorPosition(SyntaxNode node);
    }
}
