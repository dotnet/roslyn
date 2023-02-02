// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated backing field for a primary constructor parameter.
    /// </summary>
    internal sealed class SynthesizedPrimaryConstructorParameterBackingFieldSymbol : SynthesizedBackingFieldSymbolBase
    {
        public readonly ParameterSymbol ParameterSymbol;

        public SynthesizedPrimaryConstructorParameterBackingFieldSymbol(
            ParameterSymbol parameterSymbol,
            string name)
            : base(name, isReadOnly: false, isStatic: false)
        {
            ParameterSymbol = parameterSymbol;
        }

        internal override bool HasInitializer => true;

        protected override IAttributeTargetSymbol AttributeOwner
            => this;

        internal override Location ErrorLocation
            => ParameterSymbol.Locations.FirstOrDefault() ?? NoLocation.Singleton;

        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => default;

        public override Symbol? AssociatedSymbol
            => null;

        public override ImmutableArray<Location> Locations
            => ParameterSymbol.Locations;

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            => ParameterSymbol.TypeWithAnnotations;

        // PROTOTYPE(PrimaryConstructors): Some implementations (like in SourceFieldSymbolWithSyntaxReference)
        //                                 try to detect this fact from syntax. It looks like the motivation
        //                                 is to avoid some kind of circularity. Should we do the same?
        internal override bool HasPointerType
            => base.HasPointerType;

        public override Symbol ContainingSymbol
            => ParameterSymbol.ContainingSymbol.ContainingSymbol;

        public override NamedTypeSymbol ContainingType
            => ParameterSymbol.ContainingSymbol.ContainingType;

        // PROTOTYPE(PrimaryConstructors): Test synthesized attributes.
    }
}
