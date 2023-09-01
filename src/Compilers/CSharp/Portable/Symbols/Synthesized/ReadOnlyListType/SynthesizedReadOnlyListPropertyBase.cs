// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SynthesizedReadOnlyListPropertyBase : PropertySymbol
    {
        protected readonly SynthesizedReadOnlyListTypeSymbol _containingType;

        internal SynthesizedReadOnlyListPropertyBase(SynthesizedReadOnlyListTypeSymbol containingType)
        {
            _containingType = containingType;
        }

        public sealed override RefKind RefKind => RefKind.None;

        public abstract override TypeWithAnnotations TypeWithAnnotations { get; }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public abstract override ImmutableArray<ParameterSymbol> Parameters { get; }

        public abstract override bool IsIndexer { get; }

        public abstract override MethodSymbol? GetMethod { get; }

        public abstract override MethodSymbol? SetMethod { get; }

        public abstract override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations { get; }

        public sealed override Symbol ContainingSymbol => _containingType;

        public sealed override ImmutableArray<Location> Locations => _containingType.Locations;

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public abstract override Accessibility DeclaredAccessibility { get; }

        public sealed override bool IsStatic => false;

        public sealed override bool IsVirtual => false;

        public sealed override bool IsOverride => false;

        public sealed override bool IsAbstract => false;

        public sealed override bool IsSealed => false;

        public sealed override bool IsExtern => false;

        internal sealed override bool IsRequired => false;

        internal sealed override bool HasSpecialName => false;

        internal abstract override Cci.CallingConvention CallingConvention { get; }

        internal sealed override bool MustCallMethodsDirectly => false;

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData => null;
    }
}
