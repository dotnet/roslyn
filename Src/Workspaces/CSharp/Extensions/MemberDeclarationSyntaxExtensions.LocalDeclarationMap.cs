// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class MemberDeclarationSyntaxExtensions
    {
        public struct LocalDeclarationMap
        {
            private readonly Dictionary<string, ImmutableArray<SyntaxToken>> dictionary;

            internal LocalDeclarationMap(Dictionary<string, ImmutableArray<SyntaxToken>> dictionary)
            {
                this.dictionary = dictionary;
            }

            public ImmutableArray<SyntaxToken> this[string identifier]
            {
                get
                {
                    ImmutableArray<SyntaxToken> result;
                    return dictionary.TryGetValue(identifier, out result)
                        ? result
                        : ImmutableArray.Create<SyntaxToken>();
                }
            }
        }
    }
}