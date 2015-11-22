// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            : base(containingEnum, WellKnownMemberNames.EnumBackingFieldName, isPublic: true, isReadOnly: false, isStatic: false)
        {
        }

        internal override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return TypeSymbolWithAnnotations.Create(((SourceNamedTypeSymbol)ContainingType).EnumUnderlyingType);
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            // no attributes should be emitted
        }
    }
}
