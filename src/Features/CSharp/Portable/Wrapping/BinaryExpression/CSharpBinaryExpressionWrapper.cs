﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Wrapping.BinaryExpression;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.BinaryExpression
{
    internal class CSharpBinaryExpressionWrapper : AbstractBinaryExpressionWrapper<BinaryExpressionSyntax>
    {
        public CSharpBinaryExpressionWrapper()
            : base(CSharpIndentationService.Instance, CSharpSyntaxFacts.Instance, CSharpPrecedenceService.Instance)
        {
        }

        protected override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
            => newLine;
    }
}
