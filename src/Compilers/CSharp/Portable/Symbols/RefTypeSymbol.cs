// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>Represents a 'ref'/'in'/'out' type. Should only be used in delegate type arguments.</summary>
    internal partial class RefTypeSymbol : TypeSymbol
    {
        public RefKind RefKind { get; }
        public TypeWithAnnotations ReferencedTypeWithAnnotations { get; }

        public RefTypeSymbol(RefKind refKind, TypeWithAnnotations referencedTypeWithAnnotations)
        {
            RefKind = refKind;
            ReferencedTypeWithAnnotations = referencedTypeWithAnnotations;
        }

        public override bool IsReferenceType => false;
        public override bool IsValueType => false;

        public override TypeKind TypeKind => TypeKind.Ref;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => false;

        public override SymbolKind Kind => SymbolKind.RefType;

        public override Symbol? ContainingSymbol => null;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override Accessibility DeclaredAccessibility => throw ExceptionUtilities.Unreachable;

        public override bool IsStatic => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        internal override NamedTypeSymbol? BaseTypeNoUseSiteDiagnostics => null;

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            ReferencedTypeWithAnnotations.Type.Accept(visitor);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return ReferencedTypeWithAnnotations.Type.Accept(visitor);
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected override ISymbol CreateISymbol()
        {
            // PROTOTYPE(delegate-type-args): create a real public symbol?
            return ReferencedTypeWithAnnotations.Type.ISymbol;
        }

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            return ReferencedTypeWithAnnotations.Type.GetITypeSymbol(default); // PROTOTYPE(delegate-type-args)
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a)
        {
            return ReferencedTypeWithAnnotations.Type.Accept(visitor, a);
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            ReferencedTypeWithAnnotations.Type.AddNullableTransforms(transforms);
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            return ReferencedTypeWithAnnotations.Type.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out result);
        }

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
