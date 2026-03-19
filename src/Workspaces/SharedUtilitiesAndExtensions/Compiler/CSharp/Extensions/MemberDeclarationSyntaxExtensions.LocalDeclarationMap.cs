// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal partial class MemberDeclarationSyntaxExtensions
{
    public readonly struct LocalDeclarationMap
    {
        private readonly Dictionary<string, ImmutableArray<SyntaxToken>> _dictionary;

        internal LocalDeclarationMap(Dictionary<string, ImmutableArray<SyntaxToken>> dictionary)
            => _dictionary = dictionary;

        public ImmutableArray<SyntaxToken> this[string identifier]
        {
            get
            {
                return _dictionary.TryGetValue(identifier, out var result)
                    ? result
                    : [];
            }
        }
    }
}
