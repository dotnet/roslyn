// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="TypeSymbol"/> implementation that exists only to allow types and function signatures
    /// to be treated similarly in <see cref="ConversionsBase"/>, <see cref="BestTypeInferrer"/>, and
    /// <see cref="MethodTypeInferrer"/>. Instances of this type should not be used or observable
    /// outside of those code paths.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class FunctionTypeSymbol : TypeSymbol
    {
        internal static readonly FunctionTypeSymbol Uninitialized = new FunctionTypeSymbol(ErrorTypeSymbol.UnknownResultType);

        private readonly NamedTypeSymbol _delegateType;

        internal FunctionTypeSymbol(NamedTypeSymbol delegateType)
        {
            _delegateType = delegateType;
        }

        internal NamedTypeSymbol GetInternalDelegateType() => _delegateType;

        public override bool IsReferenceType => true;

        public override bool IsValueType => false;

        public override TypeKind TypeKind => TypeKindInternal.FunctionType;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => true;

        public override SymbolKind Kind => SymbolKindInternal.FunctionType;

        public override Symbol? ContainingSymbol => null;

        public override ImmutableArray<Location> Locations => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw ExceptionUtilities.Unreachable;

        public override Accessibility DeclaredAccessibility => throw ExceptionUtilities.Unreachable;

        public override bool IsStatic => false;

        public override bool IsAbstract => throw ExceptionUtilities.Unreachable;

        public override bool IsSealed => throw ExceptionUtilities.Unreachable;

        internal override NamedTypeSymbol? BaseTypeNoUseSiteDiagnostics => null;

        internal override bool IsRecord => throw ExceptionUtilities.Unreachable;

        internal override bool IsRecordStruct => throw ExceptionUtilities.Unreachable;

        internal override ObsoleteAttributeData ObsoleteAttributeData => throw ExceptionUtilities.Unreachable;

        public override void Accept(CSharpSymbolVisitor visitor) => throw ExceptionUtilities.Unreachable;

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<Symbol> GetMembers() => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<Symbol> GetMembers(string name) => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => throw ExceptionUtilities.Unreachable;

        protected override ISymbol CreateISymbol() => throw ExceptionUtilities.Unreachable;

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation) => throw ExceptionUtilities.Unreachable;

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => throw ExceptionUtilities.Unreachable;

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms) => throw ExceptionUtilities.Unreachable;

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result) => throw ExceptionUtilities.Unreachable;

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => throw ExceptionUtilities.Unreachable;

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes) => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => throw ExceptionUtilities.Unreachable;

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            return new FunctionTypeSymbol((NamedTypeSymbol)_delegateType.MergeEquivalentTypes(((FunctionTypeSymbol)other)._delegateType, variance));
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            return new FunctionTypeSymbol((NamedTypeSymbol)_delegateType.SetNullabilityForReferenceTypes(transform));
        }

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => throw ExceptionUtilities.Unreachable;

        internal override bool Equals(TypeSymbol t2, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, t2))
            {
                return true;
            }

            var otherType = (t2 as FunctionTypeSymbol)?._delegateType;
            return _delegateType.Equals(otherType, compareKind);
        }

        public override int GetHashCode()
        {
            return _delegateType.GetHashCode();
        }

        internal override string GetDebuggerDisplay()
        {
            return $"FunctionTypeSymbol: {_delegateType.GetDebuggerDisplay()}";
        }
    }
}
