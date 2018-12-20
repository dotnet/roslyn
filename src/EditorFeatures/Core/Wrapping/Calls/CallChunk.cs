// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Wrapping.Call
{
    internal abstract partial class AbstractCallWrapper<
        TExpressionSyntax,
        TNameSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TElementAccessExpressionSyntax,
        TBaseArgumentListSyntax>
    {
        private readonly struct Chunk
        {
            // Optional as VB allows an initial dotted expression starting with <dot>
            // in a `with` block.
            public readonly TExpressionSyntax ExpressionOpt;
            public readonly SyntaxToken DotToken;
            public readonly TNameSyntax Name;
            // Optional when we just have a member access expression along the way.
            public readonly TBaseArgumentListSyntax ArgumentListOpt;

            public Chunk(TExpressionSyntax expressionOpt, SyntaxToken dotToken, TNameSyntax name, TBaseArgumentListSyntax argumentListOpt)
            {
                ExpressionOpt = expressionOpt;
                DotToken = dotToken;
                Name = name;
                ArgumentListOpt = argumentListOpt;
            }
        }
    }
}
