// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Wrapping.BinaryExpression;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.BinaryExpression
{
    internal class CSharpBinaryExpressionWrapper : AbstractBinaryExpressionWrapper<BinaryExpressionSyntax>
    {
        public CSharpBinaryExpressionWrapper()
            : base(CSharpIndentationService.Instance, CSharpSyntaxFactsService.Instance, CSharpPrecedenceService.Instance)
        {
        }

        protected override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
            => newLine;
    }
}
