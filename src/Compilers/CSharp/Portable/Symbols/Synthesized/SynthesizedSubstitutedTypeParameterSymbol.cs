// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A type parameter for a synthesized class or method.
    /// </summary>
    internal sealed class SynthesizedSubstitutedTypeParameterSymbol : SubstitutedTypeParameterSymbol
    {
        public SynthesizedSubstitutedTypeParameterSymbol(Symbol owner, TypeMap map, TypeParameterSymbol substitutedFrom, int ordinal)
            : base(owner, map, substitutedFrom, ordinal)
        {
            Debug.Assert(this.TypeParameterKind == (ContainingSymbol is MethodSymbol ? TypeParameterKind.Method :
                                                   (ContainingSymbol is NamedTypeSymbol ? TypeParameterKind.Type :
                                                   TypeParameterKind.Cref)),
                         $"Container is {ContainingSymbol?.Kind}, TypeParameterKind is {this.TypeParameterKind}");
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        public override TypeParameterKind TypeParameterKind => ContainingSymbol is MethodSymbol ? TypeParameterKind.Method : TypeParameterKind.Type;

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (this.HasUnmanagedTypeConstraint)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsUnmanagedAttribute(this));
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (ContainingSymbol is SynthesizedMethodBaseSymbol { InheritsBaseMethodAttributes: true })
            {
                return _underlyingTypeParameter.GetAttributes();
            }

            return ImmutableArray<CSharpAttributeData>.Empty;
        }
    }
}
