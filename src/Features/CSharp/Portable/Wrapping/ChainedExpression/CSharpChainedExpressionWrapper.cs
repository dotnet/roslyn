// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Wrapping.ChainedExpression;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.ChainedExpression
{
    internal class CSharpChainedExpressionWrapper :
        AbstractChainedExpressionWrapper<NameSyntax, BaseArgumentListSyntax>
    {
        public CSharpChainedExpressionWrapper()
            : base(CSharpIndentationService.Instance, CSharpSyntaxFactsService.Instance)
        {
        }

        protected override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
            => newLine;
    }
}
