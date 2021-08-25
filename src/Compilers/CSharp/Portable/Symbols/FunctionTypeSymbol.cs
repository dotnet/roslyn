// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class FunctionTypeSymbol : TypeSymbol
    {
        private const SymbolKind s_SymbolKind = SymbolKind.FunctionPointerType + 1;
        private const TypeKind s_TypeKind = TypeKind.FunctionPointer + 1;

        private readonly AssemblySymbol _assembly;

        private Binder? _binder;
        private BoundMethodGroup? _methodGroup;
        private NamedTypeSymbol? _lazyDelegateType;

        internal FunctionTypeSymbol(AssemblySymbol assembly) :
            this(assembly, ErrorTypeSymbol.UnknownResultType)
        {
        }

        internal FunctionTypeSymbol(AssemblySymbol assembly, NamedTypeSymbol? delegateType)
        {
            _assembly = assembly;
            _lazyDelegateType = delegateType;
        }

        internal void SetMethodGroup(Binder binder, BoundMethodGroup methodGroup)
        {
            Debug.Assert(_binder is null);
            Debug.Assert(_methodGroup is null);
            Debug.Assert((object?)_lazyDelegateType == ErrorTypeSymbol.UnknownResultType);

            _binder = binder;
            _methodGroup = methodGroup;
        }

        internal NamedTypeSymbol? GetInternalDelegateType()
        {
            if ((object?)_lazyDelegateType == ErrorTypeSymbol.UnknownResultType)
            {
                var delegateType = _binder!.GetMethodGroupDelegateType(_methodGroup!, out _);
                Interlocked.CompareExchange(ref _lazyDelegateType, delegateType, ErrorTypeSymbol.UnknownResultType);
            }
            return _lazyDelegateType;
        }

        public override bool IsReferenceType => true;

        public override bool IsValueType => false;

        public override TypeKind TypeKind => s_TypeKind;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => true;

        public override SymbolKind Kind => s_SymbolKind;

        public override Symbol ContainingSymbol => _assembly;

        public override ImmutableArray<Location> Locations => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw ExceptionUtilities.Unreachable;

        public override Accessibility DeclaredAccessibility => throw ExceptionUtilities.Unreachable;

        public override bool IsStatic => false;

        public override bool IsAbstract => throw ExceptionUtilities.Unreachable;

        public override bool IsSealed => throw ExceptionUtilities.Unreachable;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _assembly.GetSpecialType(SpecialType.System_Object);

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

            var otherType = (FunctionTypeSymbol)other;
            var delegateType = GetInternalDelegateType();
            var otherDelegateType = otherType.GetInternalDelegateType();

            Debug.Assert((object)_assembly == otherType._assembly);
            Debug.Assert(delegateType is { });
            Debug.Assert(otherDelegateType is { });

            delegateType = (NamedTypeSymbol)delegateType.MergeEquivalentTypes(otherDelegateType, variance);
            return new FunctionTypeSymbol(_assembly, delegateType);
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            var delegateType = (NamedTypeSymbol?)GetInternalDelegateType()?.SetNullabilityForReferenceTypes(transform);
            return new FunctionTypeSymbol(_assembly, delegateType);
        }

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => throw ExceptionUtilities.Unreachable;

        internal override bool Equals(TypeSymbol t2, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, t2))
            {
                return true;
            }

            var delegateType = GetInternalDelegateType();
            if (delegateType is null)
            {
                return false;
            }

            var otherType = (t2 as FunctionTypeSymbol)?.GetInternalDelegateType();
            return delegateType.Equals(otherType, compareKind);
        }

        public override int GetHashCode()
        {
            var delegateType = GetInternalDelegateType();
            return delegateType is null ? 0 : delegateType.GetHashCode();
        }

        internal override string GetDebuggerDisplay()
        {
            return $"DelegateType: {GetInternalDelegateType()?.ToDisplayString(s_debuggerDisplayFormat) ?? "<null>"}";
        }
    }
}
