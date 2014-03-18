// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A type parameter for a synthesized class or method.
    /// </summary>
    internal sealed class SynthesizedTypeParameterSymbol : SubstitutedTypeParameterSymbol
    {
        public SynthesizedTypeParameterSymbol(Symbol owner, TypeMap map, TypeParameterSymbol substitutedFrom)
            : base(owner, map, substitutedFrom)
        {
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }
    }
}
