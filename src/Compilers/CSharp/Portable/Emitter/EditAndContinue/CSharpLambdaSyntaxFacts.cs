// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override SyntaxNode TryGetCorrespondingLambdaBody(SyntaxNode previousLambdaSyntax, SyntaxNode lambdaOrLambdaBodySyntax)
            => LambdaUtilities.TryGetCorrespondingLambdaBody(lambdaOrLambdaBodySyntax, previousLambdaSyntax);

        public override int GetDeclaratorPosition(SyntaxNode node)
            => LambdaUtilities.GetDeclaratorPosition(node);
    }
}
