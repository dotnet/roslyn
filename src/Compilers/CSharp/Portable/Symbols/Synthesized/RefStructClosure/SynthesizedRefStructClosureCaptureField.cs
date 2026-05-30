// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A ref field of a <see cref="SynthesizedRefStructClosureTypeSymbol"/> that captures an
    /// outer local or parameter by reference. Reads and writes through the field are observed
    /// by the original storage location.
    /// </summary>
    internal sealed class SynthesizedRefStructClosureCaptureField : SynthesizedFieldSymbolBase
    {
        private readonly TypeWithAnnotations _type;
        private readonly RefKind _refKind;

        internal SynthesizedRefStructClosureCaptureField(
            NamedTypeSymbol containingType,
            string name,
            TypeWithAnnotations type,
            RefKind refKind)
            : base(containingType, name, DeclarationModifiers.Public, isReadOnly: false, isStatic: false)
        {
            _type = type;
            _refKind = refKind;
        }

        public override RefKind RefKind => _refKind;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override bool SuppressDynamicAttribute => true;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound) => _type;
    }
}
