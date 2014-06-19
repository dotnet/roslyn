// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Abstract base class for mutable and immutable type maps.
    /// </summary>
    internal abstract class AbstractTypeMap
    {
        /// <summary>
        /// Substitute for a type declaration.  May use alpha renaming if the container is substituted.
        /// </summary>
        private NamedTypeSymbol SubstituteMemberType(NamedTypeSymbol previous)
        {
            Debug.Assert((object)previous.ConstructedFrom == (object)previous);

            NamedTypeSymbol newContainingType = SubstituteNamedType(previous.ContainingType);
            if ((object)newContainingType == null)
            {
                return previous;
            }

            return previous.OriginalDefinition.AsMember(newContainingType);
        }

        /// <summary>
        /// SubstType, but for NamedTypeSymbols only.  This is used for concrete types, so no alpha substitution appears in the result.
        /// </summary>
        internal NamedTypeSymbol SubstituteNamedType(NamedTypeSymbol previous)
        {
            if (ReferenceEquals(previous, null))
                return null;

            if (previous.IsUnboundGenericType)
                return previous;

            if (previous.IsAnonymousType)
            {
                ImmutableArray<TypeSymbol> oldFieldTypes = AnonymousTypeManager.GetAnonymousTypePropertyTypes(previous);
                ImmutableArray<TypeSymbol> newFieldTypes = SubstituteTypes(oldFieldTypes);
                return (oldFieldTypes == newFieldTypes) ? previous : AnonymousTypeManager.ConstructAnonymousTypeSymbol(previous, newFieldTypes);
            }

            // TODO: we could construct the result's ConstructedFrom lazily by using a "deep"
            // construct operation here (as VB does), thereby avoiding alpha renaming in most cases.
            // Aleksey has shown that would reduce GC pressure if substitutions of deeply nested generics are common.
            NamedTypeSymbol oldConstructedFrom = previous.ConstructedFrom;
            NamedTypeSymbol newConstructedFrom = SubstituteMemberType(oldConstructedFrom);

            ImmutableArray<TypeSymbol> oldTypeArguments = previous.TypeArgumentsNoUseSiteDiagnostics;
            ImmutableArray<TypeSymbol> newTypeArguments = SubstituteTypes(oldTypeArguments);

            if (ReferenceEquals(oldConstructedFrom, newConstructedFrom) && oldTypeArguments == newTypeArguments)
            {
                return previous;
            }

            return newConstructedFrom.ConstructIfGeneric(newTypeArguments);
        }

        /// <summary>
        /// Perform the substitution on the given type.  Each occurrence of the type parameter is
        /// replaced with its corresponding type argument from the map.
        /// </summary>
        /// <param name="previous">The type to be rewritten.</param>
        /// <returns>The type with type parameters replaced with the type arguments.</returns>
        internal TypeSymbol SubstituteType(TypeSymbol previous)
        {
            if (ReferenceEquals(previous, null))
                return null;

            switch (previous.Kind)
            {
                case SymbolKind.NamedType:
                    return SubstituteNamedType((NamedTypeSymbol)previous);
                case SymbolKind.TypeParameter:
                    return SubstituteTypeParameter((TypeParameterSymbol)previous);
                case SymbolKind.ArrayType:
                    return SubstituteArrayType((ArrayTypeSymbol)previous);
                case SymbolKind.PointerType:
                    return SubstitutePointerType((PointerTypeSymbol)previous);
                case SymbolKind.DynamicType:
                    return SubstituteDynamicType();
                case SymbolKind.ErrorType:
                    return ((ErrorTypeSymbol)previous).Substitute(this);
                default:
                    return previous;
            }
        }

        protected virtual TypeSymbol SubstituteDynamicType()
        {
            return DynamicTypeSymbol.Instance;
        }

        protected virtual TypeSymbol SubstituteTypeParameter(TypeParameterSymbol typeParameter)
        {
            return typeParameter;
        }

        private ArrayTypeSymbol SubstituteArrayType(ArrayTypeSymbol t)
        {
            TypeSymbol element = SubstituteType(t.ElementType);
            if (ReferenceEquals(element, t.ElementType))
            {
                return t;
            }

            ImmutableArray<NamedTypeSymbol> interfaces = t.InterfacesNoUseSiteDiagnostics;
            Debug.Assert(0 <= interfaces.Length && interfaces.Length <= 2);

            if (interfaces.Length == 1)
            {
                Debug.Assert(interfaces[0] is NamedTypeSymbol); // IList<T>
                interfaces = ImmutableArray.Create<NamedTypeSymbol>((NamedTypeSymbol)SubstituteType(interfaces[0]));
            }
            else if (interfaces.Length == 2)
            {
                Debug.Assert(interfaces[0] is NamedTypeSymbol); // IList<T>
                interfaces = ImmutableArray.Create<NamedTypeSymbol>((NamedTypeSymbol)SubstituteType(interfaces[0]), (NamedTypeSymbol)SubstituteType(interfaces[1]));
            }

            return new ArrayTypeSymbol(
                element,
                t.Rank,
                t.BaseTypeNoUseSiteDiagnostics,
                interfaces,
                t.CustomModifiers);
        }

        private PointerTypeSymbol SubstitutePointerType(PointerTypeSymbol t)
        {
            TypeSymbol pointedAtType = SubstituteType(t.PointedAtType);
            if (ReferenceEquals(pointedAtType, t.PointedAtType))
            {
                return t;
            }

            return new PointerTypeSymbol(pointedAtType, t.CustomModifiers);
        }

        internal ImmutableArray<TypeSymbol> SubstituteTypes(ImmutableArray<TypeSymbol> original)
        {
            if (original.IsDefault)
            {
                return original;
            }

            TypeSymbol[] result = null;

            for (int i = 0; i < original.Length; i++)
            {
                var t = original[i];
                var substituted = SubstituteType(t);
                if (!Object.ReferenceEquals(substituted, t))
                {
                    if (result == null)
                    {
                        result = new TypeSymbol[original.Length];
                        for (int j = 0; j < i; j++)
                        {
                            result[j] = original[j];
                        }
                    }
                }

                if (result != null)
                {
                    result[i] = substituted;
                }
            }

            return result != null ? result.AsImmutableOrNull() : original;
        }

        /// <summary>
        /// Substitute types, and return the results without duplicates, preserving the original order.
        /// </summary>
        internal void SubstituteTypesDistinct(ImmutableArray<TypeSymbol> original, ArrayBuilder<TypeSymbol> result)
        {
            if (original.Length == 0)
            {
                return;
            }
            else if (original.Length == 1)
            {
                result.Add(SubstituteType(original[0]));
            }
            else
            {
                var set = new HashSet<TypeSymbol>();
                foreach (var type in original)
                {
                    var substituted = SubstituteType(type);
                    if (set.Add(substituted))
                    {
                        result.Add(substituted);
                    }
                }
            }
        }

        internal ImmutableArray<TypeParameterSymbol> SubstituteTypeParameters(ImmutableArray<TypeParameterSymbol> original)
        {
            return original.SelectAsArray((tp, m) => (TypeParameterSymbol)m.SubstituteTypeParameter(tp), this);
        }

        /// <summary>
        /// Like SubstTypes, but for NamedTypeSymbols.
        /// </summary>
        internal ImmutableArray<NamedTypeSymbol> SubstituteNamedTypes(ImmutableArray<NamedTypeSymbol> original)
        {
            NamedTypeSymbol[] result = null;

            for (int i = 0; i < original.Length; i++)
            {
                var t = original[i];
                var substituted = SubstituteNamedType(t);
                if (!Object.ReferenceEquals(substituted, t))
                {
                    if (result == null)
                    {
                        result = new NamedTypeSymbol[original.Length];
                        for (int j = 0; j < i; j++)
                        {
                            result[j] = original[j];
                        }
                    }
                }

                if (result != null)
                {
                    result[i] = substituted;
                }
            }

            return result != null ? result.AsImmutableOrNull() : original;
        }
    }
}
