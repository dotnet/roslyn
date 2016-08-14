// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal class CSharpLambdaSyntaxHelper : ILambdaSyntaxHelper
    {
        public static readonly ILambdaSyntaxHelper Instance = new CSharpLambdaSyntaxHelper();

        private CSharpLambdaSyntaxHelper()
        {
        }

        public SyntaxNode GetLambda(SyntaxNode lambdaOrLambdaBodySyntax)
        {
            return LambdaUtilities.GetLambda(lambdaOrLambdaBodySyntax);
        }

        public SyntaxNode TryGetCorrespondingLambdaBody(
            SyntaxNode previousLambdaSyntax,
            SyntaxNode lambdaOrLambdaBodySyntax)
        {
            return LambdaUtilities.TryGetCorrespondingLambdaBody(
                lambdaOrLambdaBodySyntax, previousLambdaSyntax);
        }
    }
}