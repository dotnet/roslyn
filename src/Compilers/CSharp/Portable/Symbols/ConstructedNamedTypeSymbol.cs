// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A named type symbol that results from substituting a new owner for a type declaration.
    /// </summary>
    internal sealed class SubstitutedNestedTypeSymbol : SubstitutedNamedTypeSymbol
    {
        internal SubstitutedNestedTypeSymbol(SubstitutedNamedTypeSymbol newContainer, NamedTypeSymbol originalDefinition)
            : base(
                newContainer: newContainer,
                map: newContainer.TypeSubstitution,
                originalDefinition: originalDefinition,
                // An Arity-0 member of an unbound type, e.g. A<>.B, is unbound.
                unbound: newContainer.IsUnboundGenericType && originalDefinition.Arity == 0)
        {
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
            => OriginalDefinition.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }

        public sealed override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// A generic named type symbol that has been constructed with type arguments distinct from its own type parameters.
    /// </summary>
    internal sealed class ConstructedNamedTypeSymbol : SubstitutedNamedTypeSymbol
    {
        private readonly ImmutableArray<TypeWithAnnotations> _typeArgumentsWithAnnotations;
        private readonly NamedTypeSymbol _constructedFrom;

        internal ConstructedNamedTypeSymbol(NamedTypeSymbol constructedFrom, ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations, bool unbound = false, TupleExtraData tupleData = null)
            : base(newContainer: constructedFrom.ContainingSymbol,
                   map: new TypeMap(constructedFrom.ContainingType, constructedFrom.OriginalDefinition.TypeParameters, typeArgumentsWithAnnotations),
                   originalDefinition: constructedFrom.OriginalDefinition,
                   constructedFrom: constructedFrom, unbound: unbound, tupleData: tupleData)
        {
            _typeArgumentsWithAnnotations = typeArgumentsWithAnnotations;
            _constructedFrom = constructedFrom;

            Debug.Assert(constructedFrom.Arity == typeArgumentsWithAnnotations.Length);
            Debug.Assert(constructedFrom.Arity != 0);
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            return new ConstructedNamedTypeSymbol(_constructedFrom, _typeArgumentsWithAnnotations, IsUnboundGenericType, tupleData: newData);
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return _constructedFrom;
            }
        }

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get
            {
                return _typeArgumentsWithAnnotations;
            }
        }

        internal static bool TypeParametersMatchTypeArguments(ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            int n = typeParameters.Length;
            Debug.Assert(typeArguments.Length == n);
            Debug.Assert(typeArguments.Length > 0);

            for (int i = 0; i < n; i++)
            {
                if (!typeArguments[i].Is(typeParameters[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal sealed override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            if (ConstructedFrom.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                GetUnificationUseSiteDiagnosticRecursive(ref result, _typeArgumentsWithAnnotations, owner, ref checkedTypes))
            {
                return true;
            }

            var typeArguments = this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            foreach (var typeArg in typeArguments)
            {
                if (GetUnificationUseSiteDiagnosticRecursive(ref result, typeArg.CustomModifiers, owner, ref checkedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        public sealed override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }
    }
}
