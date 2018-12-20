// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Wrapping.BinaryExpression;
using Microsoft.CodeAnalysis.Editor.Wrapping.Call;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Wrapping.Call
{
    internal class CSharpCallWrapper : AbstractCallWrapper<
        ExpressionSyntax,
        NameSyntax,
        MemberAccessExpressionSyntax,
        InvocationExpressionSyntax,
        ElementAccessExpressionSyntax,
        BaseArgumentListSyntax>
    {
        public CSharpCallWrapper()
            : base(CSharpSyntaxFactsService.Instance)
        {
        }

        public override SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine)
            => newLine;
    }
}
