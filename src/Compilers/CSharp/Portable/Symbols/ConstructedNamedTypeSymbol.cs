// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics
        {
            get { return TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations); }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }
    }

    /// <summary>
    /// A generic named type symbol that has been constructed with type arguments distinct from its own type parameters.
    /// </summary>
    internal sealed class ConstructedNamedTypeSymbol : SubstitutedNamedTypeSymbol
    {
        private readonly ImmutableArray<TypeSymbolWithAnnotations> _typeArguments;
        private readonly NamedTypeSymbol _constructedFrom;

        internal ConstructedNamedTypeSymbol(NamedTypeSymbol constructedFrom, ImmutableArray<TypeSymbolWithAnnotations> typeArguments, bool unbound = false)
            : base(newContainer: constructedFrom.ContainingSymbol,
                   map: new TypeMap(constructedFrom.ContainingType, constructedFrom.OriginalDefinition.TypeParameters, typeArguments),
                   originalDefinition: constructedFrom.OriginalDefinition,
                   constructedFrom: constructedFrom, unbound: unbound)
        {
            _typeArguments = typeArguments;
            _constructedFrom = constructedFrom;

            Debug.Assert(constructedFrom.Arity == typeArguments.Length);
            Debug.Assert(constructedFrom.Arity != 0);
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return _constructedFrom;
            }
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return _typeArguments;
            }
        }

        internal static bool TypeParametersMatchTypeArguments(ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeSymbolWithAnnotations> typeArguments)
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
            return ConstructedFrom.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, _typeArguments, owner, ref checkedTypes);
        }
    }
}
