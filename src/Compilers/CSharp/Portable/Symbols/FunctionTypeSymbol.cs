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
    internal sealed class FunctionTypeSymbol : TypeSymbol
    {
        internal const SymbolKind FunctionType_SymbolKind = SymbolKind.FunctionPointerType + 1;
        internal const TypeKind FunctionType_TypeKind = TypeKind.FunctionPointer + 1;

        // PROTOTYPE: Should be a singleton instance on SourceAssemblySymbol.
        internal static FunctionTypeSymbol CreateErrorType(AssemblySymbol assembly) => new FunctionTypeSymbol(assembly, delegateType: null);

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

        public override TypeKind TypeKind => FunctionType_TypeKind;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => true;

        public override SymbolKind Kind => FunctionType_SymbolKind;

        public override Symbol ContainingSymbol => _assembly;

        public override ImmutableArray<Location> Locations => throw new NotImplementedException();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();

        public override Accessibility DeclaredAccessibility => throw new NotImplementedException();

        public override bool IsStatic => false;

        public override bool IsAbstract => throw new NotImplementedException();

        public override bool IsSealed => throw new NotImplementedException();

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _assembly.GetSpecialType(SpecialType.System_Object);

        internal override bool IsRecord => throw new NotImplementedException();

        internal override bool IsRecordStruct => throw new NotImplementedException();

        internal override ObsoleteAttributeData ObsoleteAttributeData => throw new NotImplementedException();

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            throw new NotImplementedException();
        }

        protected override ISymbol CreateISymbol()
        {
            throw new NotImplementedException();
        }

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            throw new NotImplementedException();
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a)
        {
            throw new NotImplementedException();
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            throw new NotImplementedException();
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            throw new NotImplementedException();
        }

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            throw new NotImplementedException();
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null)
        {
            throw new NotImplementedException();
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            throw new NotImplementedException();
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform) => this;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            throw new NotImplementedException();
        }

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
    }
}
