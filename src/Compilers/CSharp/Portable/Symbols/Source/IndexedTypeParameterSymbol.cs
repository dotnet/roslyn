// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Indexed type parameters are used in place of type parameters for method signatures.  There is
    /// a unique mapping from index to a single IndexedTypeParameterSymbol.  
    /// 
    /// They don't have a containing symbol or locations.
    /// 
    /// They do not have constraints, variance, or attributes. 
    /// </summary>
    internal sealed class IndexedTypeParameterSymbol : TypeParameterSymbol
    {
        private static TypeParameterSymbol[] s_parameterPool = Array.Empty<TypeParameterSymbol>();

        private readonly int _index;

        private IndexedTypeParameterSymbol(int index)
        {
            _index = index;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Method;
            }
        }

        internal static TypeParameterSymbol GetTypeParameter(int index)
        {
            if (index >= s_parameterPool.Length)
            {
                GrowPool(index + 1);
            }

            return s_parameterPool[index];
        }

        private static void GrowPool(int count)
        {
            var initialPool = s_parameterPool;
            while (count > initialPool.Length)
            {
                var newPoolSize = ((count + 0x0F) & ~0xF); // grow in increments of 16
                var newPool = new TypeParameterSymbol[newPoolSize];

                Array.Copy(initialPool, newPool, initialPool.Length);

                for (int i = initialPool.Length; i < newPool.Length; i++)
                {
                    newPool[i] = new IndexedTypeParameterSymbol(i);
                }

                Interlocked.CompareExchange(ref s_parameterPool, newPool, initialPool);

                // repeat if race condition occurred and someone else resized the pool before us
                // and the new pool is still too small
                initialPool = s_parameterPool;
            }
        }

        /// <summary>
        /// Create a vector of n dummy type parameters.  Always reuses the same type parameter symbol
        /// for the same position.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal static ImmutableArray<TypeParameterSymbol> Take(int count)
        {
            if (count > s_parameterPool.Length)
            {
                GrowPool(count);
            }

            ArrayBuilder<TypeParameterSymbol> builder = ArrayBuilder<TypeParameterSymbol>.GetInstance();

            for (int i = 0; i < count; i++)
            {
                builder.Add(GetTypeParameter(i));
            }

            return builder.ToImmutableAndFree();
        }

        public override int Ordinal
        {
            get { return _index; }
        }

        // These object are unique (per index).
        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
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
            get { return false; }
        }

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get { return false; }
        }

        public override bool HasNotNullConstraint => false;

        internal override bool? IsNotNullable => null;

        public override bool HasUnmanagedTypeConstraint
        {
            get { return false; }
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

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<TypeWithAnnotations>.Empty;
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
