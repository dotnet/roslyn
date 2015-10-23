﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class MemberDeclarationSyntaxExtensions
    {
        public struct LocalDeclarationMap
        {
            private readonly Dictionary<string, ImmutableArray<SyntaxToken>> _dictionary;

            internal LocalDeclarationMap(Dictionary<string, ImmutableArray<SyntaxToken>> dictionary)
            {
                _dictionary = dictionary;
            }

            public ImmutableArray<SyntaxToken> this[string identifier]
            {
                get
                {
                    ImmutableArray<SyntaxToken> result;
                    return _dictionary.TryGetValue(identifier, out result)
                        ? result
                        : ImmutableArray.Create<SyntaxToken>();
                }
            }
        }
    }
}
