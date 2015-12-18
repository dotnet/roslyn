// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Indexed type parameters are used in place of type parameters for method signatures. 
    /// 
    /// They don't have a containing symbol or locations.
    /// 
    /// They do not have constraints (except reference type constraint), variance, or attributes. 
    /// </summary>
    internal sealed class IndexedTypeParameterSymbolForOverriding : TypeParameterSymbol
    {
        private static TypeParameterSymbol[] s_parameterPoolHasReferenceTypeConstraint = SpecializedCollections.EmptyArray<TypeParameterSymbol>();
        private static TypeParameterSymbol[] s_parameterPoolHasNoReferenceTypeConstraint = SpecializedCollections.EmptyArray<TypeParameterSymbol>();

        private readonly int _index;
        private readonly bool _hasReferenceTypeConstraint;

        private IndexedTypeParameterSymbolForOverriding(int index, bool hasReferenceTypeConstraint)
        {
            _index = index;
            _hasReferenceTypeConstraint = hasReferenceTypeConstraint;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Method;
            }
        }

        internal static TypeParameterSymbol GetTypeParameter(int index, bool hasReferenceTypeConstraint)
        {
            TypeParameterSymbol result;

            if (hasReferenceTypeConstraint)
            {
                result = GetTypeParameter(ref s_parameterPoolHasReferenceTypeConstraint, index, hasReferenceTypeConstraint);
            }
            else
            {
                result = GetTypeParameter(ref s_parameterPoolHasNoReferenceTypeConstraint, index, hasReferenceTypeConstraint);
            }

            Debug.Assert(result.HasReferenceTypeConstraint == hasReferenceTypeConstraint);
            return result;
        }

        private static TypeParameterSymbol GetTypeParameter(ref TypeParameterSymbol[] parameterPool, int index, bool hasReferenceTypeConstraint)
        {
            if (index >= parameterPool.Length)
            {
                GrowPool(ref parameterPool, index + 1, hasReferenceTypeConstraint);
            }

            return parameterPool[index];
        }

        private static void GrowPool(ref TypeParameterSymbol[] parameterPool, int count, bool hasReferenceTypeConstraint)
        {
            var initialPool = parameterPool;
            while (count > initialPool.Length)
            {
                var newPoolSize = ((count + 0x0F) & ~0xF); // grow in increments of 16
                var newPool = new TypeParameterSymbol[newPoolSize];

                Array.Copy(initialPool, newPool, initialPool.Length);

                for (int i = initialPool.Length; i < newPool.Length; i++)
                {
                    newPool[i] = new IndexedTypeParameterSymbolForOverriding(i, hasReferenceTypeConstraint);
                }

                Interlocked.CompareExchange(ref parameterPool, newPool, initialPool);

                // repeat if race condition occurred and someone else resized the pool before us
                // and the new pool is still too small
                initialPool = parameterPool;
            }
        }

        public override int Ordinal
        {
            get { return _index; }
        }

        // These object are unique (per index).
        internal override bool Equals(TypeSymbol t2, TypeSymbolEqualityOptions options)
        {
            return ReferenceEquals(this, t2);
        }

        public override int GetHashCode()
        {
            return _index;
        }

        public override VarianceKind Variance
        {
            get { return VarianceKind.None; }
        }

        public override bool HasValueTypeConstraint
        {
            get { return false; }
        }

        public override bool HasReferenceTypeConstraint
        {
            get { return _hasReferenceTypeConstraint; }
        }

        public override bool HasConstructorConstraint
        {
            get { return false; }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return null;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            return null;
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            return null;
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }
    }
}
