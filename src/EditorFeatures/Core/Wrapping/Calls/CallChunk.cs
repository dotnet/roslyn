// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
        private readonly struct MemberChunk
        {
            public readonly SyntaxToken DotToken;
            public readonly TNameSyntax Name;

            public MemberChunk(SyntaxToken dotToken, TNameSyntax name)
            {
                DotToken = dotToken;
                Name = name;
            }

            /// <summary>
            /// The length this chunk will be once all unnecessary whitespace has been
            /// removed from it.
            /// </summary>
            public int NormalizedLength()
                => DotToken.Width() + Name.Width();
        }

        private readonly struct CallChunk
        {
            // Optional as VB allows an initial dotted expression starting with <dot>
            // in a `with` block.
            public readonly ImmutableArray<MemberChunk> MemberChunks;
            public readonly TBaseArgumentListSyntax ArgumentList;

            public CallChunk(ImmutableArray<MemberChunk> memberChunks, TBaseArgumentListSyntax argumentList)
            {
                Debug.Assert(memberChunks.Length > 0);
                MemberChunks = memberChunks;
                ArgumentList = argumentList;
            }

            /// <summary>
            /// The length this chunk will be once all unnecessary whitespace has been
            /// removed from it.
            /// </summary>
            public int NormalizedLength()
                => MemberChunks.Sum(c => c.NormalizedLength()) + ArgumentList.Width();
        }
    }
}
