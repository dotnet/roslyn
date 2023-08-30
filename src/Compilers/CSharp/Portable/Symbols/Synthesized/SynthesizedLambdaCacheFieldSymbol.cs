// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedLambdaCacheFieldSymbol : SynthesizedFieldSymbolBase, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly TypeWithAnnotations _type;
        private readonly MethodSymbol _topLevelMethod;

        public SynthesizedLambdaCacheFieldSymbol(NamedTypeSymbol containingType, TypeSymbol type, string name, MethodSymbol topLevelMethod, bool isReadOnly, bool isStatic)
            : base(containingType, name, isPublic: true, isReadOnly: isReadOnly, isStatic: isStatic)
        {
            Debug.Assert((object)type != null);
            Debug.Assert((object)topLevelMethod != null);
            _type = TypeWithAnnotations.Create(type);
            _topLevelMethod = topLevelMethod;
        }

        internal override bool SuppressDynamicAttribute => true;

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method => _topLevelMethod;

        // When the containing top-level method body is updated we don't need to attempt to update the cache field
        // since a field update is a no-op.
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => false;

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }
    }
}
