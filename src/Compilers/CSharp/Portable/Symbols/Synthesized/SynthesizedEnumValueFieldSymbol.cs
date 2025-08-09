// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents __value field of an enum.
    /// </summary>
    internal sealed class SynthesizedEnumValueFieldSymbol : SynthesizedFieldSymbolBase
    {
        public SynthesizedEnumValueFieldSymbol(SourceNamedTypeSymbol containingEnum)
            : base(containingEnum, WellKnownMemberNames.EnumBackingFieldName, DeclarationModifiers.Public, isReadOnly: false, isStatic: false)
        {
        }

        internal override bool SuppressDynamicAttribute
        {
            get { return true; }
        }

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return TypeWithAnnotations.Create(((SourceNamedTypeSymbol)ContainingType).EnumUnderlyingType);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            // no attributes should be emitted
        }
    }
}
