// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ConstructedMethodSymbol : SubstitutedMethodSymbol
    {
        private readonly ImmutableArray<TypeSymbol> typeArguments;

        internal ConstructedMethodSymbol(MethodSymbol constructedFrom, ImmutableArray<TypeSymbol> typeArguments)
            : base(containingSymbol: constructedFrom.ContainingType,
                   map: new TypeMap(constructedFrom.ContainingType, ((MethodSymbol)constructedFrom.OriginalDefinition).TypeParameters, typeArguments),
                   originalDefinition: (MethodSymbol)constructedFrom.OriginalDefinition,
                   constructedFrom: constructedFrom)
        {
            this.typeArguments = typeArguments;
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                return typeArguments;
            }
        }
    }
}
