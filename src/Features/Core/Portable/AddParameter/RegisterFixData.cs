// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
