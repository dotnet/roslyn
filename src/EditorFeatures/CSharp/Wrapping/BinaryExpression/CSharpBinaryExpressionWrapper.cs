// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Wrapping.BinaryExpression;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Wrapping.BinaryExpression
{
    internal class CSharpBinaryExpressionWrapper : AbstractBinaryExpressionWrapper<BinaryExpressionSyntax>
    {
        public CSharpBinaryExpressionWrapper()
            : base(CSharpSyntaxFactsService.Instance, CSharpPrecedenceService.Instance)
        {
        }

        protected override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
            => newLine;
    }
}
