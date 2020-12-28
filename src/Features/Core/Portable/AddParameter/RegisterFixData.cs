﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal class RegisterFixData<TArgumentSyntax>
        where TArgumentSyntax : SyntaxNode
    {
        public RegisterFixData() : this(new SeparatedSyntaxList<TArgumentSyntax>(), ImmutableArray<IMethodSymbol>.Empty, false)
        {
        }

        public RegisterFixData(SeparatedSyntaxList<TArgumentSyntax> arguments, ImmutableArray<IMethodSymbol> methodCandidates, bool isConstructorInitializer)
        {
            Arguments = arguments;
            MethodCandidates = methodCandidates;
            IsConstructorInitializer = isConstructorInitializer;
        }

        public SeparatedSyntaxList<TArgumentSyntax> Arguments { get; }
        public ImmutableArray<IMethodSymbol> MethodCandidates { get; }
        public bool IsConstructorInitializer { get; }
    }
}
