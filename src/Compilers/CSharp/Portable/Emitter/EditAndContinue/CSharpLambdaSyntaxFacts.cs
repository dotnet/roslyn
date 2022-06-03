// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal class CSharpLambdaSyntaxFacts : LambdaSyntaxFacts
    {
        public static readonly LambdaSyntaxFacts Instance = new CSharpLambdaSyntaxFacts();

        private CSharpLambdaSyntaxFacts()
        {
        }

        public override SyntaxNode GetLambda(SyntaxNode lambdaOrLambdaBodySyntax)
            => LambdaUtilities.GetLambda(lambdaOrLambdaBodySyntax);

        public override SyntaxNode? TryGetCorrespondingLambdaBody(SyntaxNode previousLambdaSyntax, SyntaxNode lambdaOrLambdaBodySyntax)
            => LambdaUtilities.TryGetCorrespondingLambdaBody(lambdaOrLambdaBodySyntax, previousLambdaSyntax);

        public override int GetDeclaratorPosition(SyntaxNode node)
            => LambdaUtilities.GetDeclaratorPosition(node);
    }
}
