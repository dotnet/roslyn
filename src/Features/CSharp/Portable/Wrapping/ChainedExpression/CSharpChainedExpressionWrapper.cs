// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Wrapping.ChainedExpression;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.ChainedExpression
{
    internal class CSharpChainedExpressionWrapper :
        AbstractChainedExpressionWrapper<NameSyntax, BaseArgumentListSyntax>
    {
        public CSharpChainedExpressionWrapper()
            : base(CSharpIndentationService.Instance, CSharpSyntaxFacts.Instance)
        {
        }

        protected override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
            => newLine;
    }
}
