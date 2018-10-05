// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
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
        private static TypeParameterSymbol[] s_parameterPoolHasValueTypeConstraint = Array.Empty<TypeParameterSymbol>();
        private static TypeParameterSymbol[] s_parameterPoolHasNoValueTypeConstraint = Array.Empty<TypeParameterSymbol>();

        private readonly int _index;
        private readonly bool _hasValueTypeConstraint;

        private IndexedTypeParameterSymbolForOverriding(int index, bool hasValueTypeConstraint)
        {
            _index = index;
            _hasValueTypeConstraint = hasValueTypeConstraint;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Method;
            }
        }

        internal static TypeParameterSymbol GetTypeParameter(int index, bool hasValueTypeConstraint)
        {
            TypeParameterSymbol result;

            if (hasValueTypeConstraint)
            {
                result = GetTypeParameter(ref s_parameterPoolHasValueTypeConstraint, index, hasValueTypeConstraint);
            }
            else
            {
                result = GetTypeParameter(ref s_parameterPoolHasNoValueTypeConstraint, index, hasValueTypeConstraint);
            }

            Debug.Assert(result.HasValueTypeConstraint == hasValueTypeConstraint);
            return result;
        }

        private static TypeParameterSymbol GetTypeParameter(ref TypeParameterSymbol[] parameterPool, int index, bool hasValueTypeConstraint)
        {
            if (index >= parameterPool.Length)
            {
                GrowPool(ref parameterPool, index + 1, hasValueTypeConstraint);
            }

            return parameterPool[index];
        }

        private static void GrowPool(ref TypeParameterSymbol[] parameterPool, int count, bool hasValueTypeConstraint)
        {
            var initialPool = parameterPool;
            while (count > initialPool.Length)
            {
                var newPoolSize = ((count + 0x0F) & ~0xF); // grow in increments of 16
                var newPool = new TypeParameterSymbol[newPoolSize];

                Array.Copy(initialPool, newPool, initialPool.Length);

                for (int i = initialPool.Length; i < newPool.Length; i++)
                {
                    newPool[i] = new IndexedTypeParameterSymbolForOverriding(i, hasValueTypeConstraint);
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
        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
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
            get { return _hasValueTypeConstraint; }
        }

        public override bool HasReferenceTypeConstraint
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override bool HasUnmanagedTypeConstraint
        {
            get { throw ExceptionUtilities.Unreachable; }
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

        internal override void EnsureAllConstraintsAreResolved(bool early)
        {
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress, bool early)
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
