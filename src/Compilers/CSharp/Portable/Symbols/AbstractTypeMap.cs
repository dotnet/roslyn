// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

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
                ImmutableArray<TypeSymbolWithAnnotations> oldFieldTypes = AnonymousTypeManager.GetAnonymousTypePropertyTypes(previous);
                ImmutableArray<TypeSymbolWithAnnotations> newFieldTypes = SubstituteTypes(oldFieldTypes);
                return (oldFieldTypes == newFieldTypes) ? previous : AnonymousTypeManager.ConstructAnonymousTypeSymbol(previous, newFieldTypes);
            }

            // TODO: we could construct the result's ConstructedFrom lazily by using a "deep"
            // construct operation here (as VB does), thereby avoiding alpha renaming in most cases.
            // Aleksey has shown that would reduce GC pressure if substitutions of deeply nested generics are common.
            NamedTypeSymbol oldConstructedFrom = previous.ConstructedFrom;
            NamedTypeSymbol newConstructedFrom = SubstituteMemberType(oldConstructedFrom);

            ImmutableArray<TypeSymbolWithAnnotations> oldTypeArguments = previous.TypeArgumentsNoUseSiteDiagnostics;
            bool changed = !ReferenceEquals(oldConstructedFrom, newConstructedFrom);

            var newTypeArguments = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(oldTypeArguments.Length);

            for (int i = 0; i < oldTypeArguments.Length; i++)
            {
                var oldArgument = oldTypeArguments[i];
                var newArgument = oldArgument.SubstituteType(this);

                if (!changed && (object)oldArgument != newArgument)
                {
                    changed = true;
                }

                newTypeArguments.Add(newArgument);
            }

            if (!changed)
            {
                newTypeArguments.Free();
                return previous;
            }

            return newConstructedFrom.ConstructIfGeneric(newTypeArguments.ToImmutableAndFree());
        }

        /// <summary>
        /// Perform the substitution on the given type.  Each occurrence of the type parameter is
        /// replaced with its corresponding type argument from the map.
        /// </summary>
        /// <param name="previous">The type to be rewritten.</param>
        /// <returns>The type with type parameters replaced with the type arguments.</returns>
        internal TypeSymbolWithAnnotations SubstituteType(TypeSymbol previous)
        {
            if (ReferenceEquals(previous, null))
                return default(TypeSymbolWithAnnotations);

            TypeSymbol result;

            switch (previous.Kind)
            {
                case SymbolKind.NamedType:
                    result = SubstituteNamedType((NamedTypeSymbol)previous);
                    break;
                case SymbolKind.TypeParameter:
                    return SubstituteTypeParameter((TypeParameterSymbol)previous);
                case SymbolKind.ArrayType:
                    result = SubstituteArrayType((ArrayTypeSymbol)previous);
                    break;
                case SymbolKind.PointerType:
                    result = SubstitutePointerType((PointerTypeSymbol)previous);
                    break;
                case SymbolKind.DynamicType:
                    result = SubstituteDynamicType();
                    break;
                case SymbolKind.ErrorType:
                    return ((ErrorTypeSymbol)previous).Substitute(this);
                default:
                    result = previous;
                    break;
            }

            return TypeSymbolWithAnnotations.Create(result);
        }

        internal TypeSymbolWithAnnotations SubstituteType(TypeSymbolWithAnnotations previous)
        {
            return previous.SubstituteType(this);
        }

        private static bool IsPossiblyByRefTypeParameter(TypeSymbol type)
        {
            if (type.IsTypeParameter())
            {
                return true;
            }

            if (type.IsErrorType())
            {
                var byRefReturnType = type as ByRefReturnErrorTypeSymbol;

                return ((object)byRefReturnType != null) && byRefReturnType.ReferencedType.IsTypeParameter();
            }

            return false;
        }

        internal ImmutableArray<CustomModifier> SubstituteCustomModifiers(ImmutableArray<CustomModifier> customModifiers)
        {
            if (customModifiers.IsDefaultOrEmpty)
            {
                return customModifiers;
            }

            for (int i = 0; i < customModifiers.Length; i++)
            {
                var modifier = (NamedTypeSymbol)customModifiers[i].Modifier;
                var substituted = SubstituteNamedType(modifier);

                if (modifier != substituted)
                {
                    var builder = ArrayBuilder<CustomModifier>.GetInstance(customModifiers.Length);
                    builder.AddRange(customModifiers, i);

                    builder.Add(customModifiers[i].IsOptional ? CSharpCustomModifier.CreateOptional(substituted) : CSharpCustomModifier.CreateRequired(substituted));
                    for (i++; i < customModifiers.Length; i++)
                    {
                        modifier = (NamedTypeSymbol)customModifiers[i].Modifier;
                        substituted = SubstituteNamedType(modifier);

                        if (modifier != substituted)
                        {
                            builder.Add(customModifiers[i].IsOptional ? CSharpCustomModifier.CreateOptional(substituted) : CSharpCustomModifier.CreateRequired(substituted));
                        }
                        else
                        {
                            builder.Add(customModifiers[i]);
                        }
                    }

                    Debug.Assert(builder.Count == customModifiers.Length);
                    return builder.ToImmutableAndFree();
                }
            }

            return customModifiers;
        }

        protected virtual TypeSymbol SubstituteDynamicType()
        {
            return DynamicTypeSymbol.Instance;
        }

        protected virtual TypeSymbolWithAnnotations SubstituteTypeParameter(TypeParameterSymbol typeParameter)
        {
            return TypeSymbolWithAnnotations.Create(typeParameter);
        }

        private ArrayTypeSymbol SubstituteArrayType(ArrayTypeSymbol t)
        {
            var oldElement = t.ElementType;
            TypeSymbolWithAnnotations element = oldElement.SubstituteType(this);
            if ((object)element == oldElement)
            {
                return t;
            }

            if (t.IsSZArray)
            {
                ImmutableArray<NamedTypeSymbol> interfaces = t.InterfacesNoUseSiteDiagnostics();
                Debug.Assert(0 <= interfaces.Length && interfaces.Length <= 2);

                if (interfaces.Length == 1)
                {
                    Debug.Assert(interfaces[0] is NamedTypeSymbol); // IList<T>
                    interfaces = ImmutableArray.Create<NamedTypeSymbol>(SubstituteNamedType(interfaces[0]));
                }
                else if (interfaces.Length == 2)
                {
                    Debug.Assert(interfaces[0] is NamedTypeSymbol); // IList<T>
                    interfaces = ImmutableArray.Create<NamedTypeSymbol>(SubstituteNamedType(interfaces[0]), SubstituteNamedType(interfaces[1]));
                }
                else if (interfaces.Length != 0)
                {
                    throw ExceptionUtilities.Unreachable;
                }

                return ArrayTypeSymbol.CreateSZArray(
                    element,
                    t.BaseTypeNoUseSiteDiagnostics,
                    interfaces);
            }

            return ArrayTypeSymbol.CreateMDArray(
                element,
                t.Rank,
                t.Sizes,
                t.LowerBounds,
                t.BaseTypeNoUseSiteDiagnostics);
        }

        private PointerTypeSymbol SubstitutePointerType(PointerTypeSymbol t)
        {
            var oldPointedAtType = t.PointedAtType;
            var pointedAtType = oldPointedAtType.SubstituteType(this);
            if ((object)pointedAtType == oldPointedAtType)
            {
                return t;
            }

            return new PointerTypeSymbol(pointedAtType);
        }

        internal ImmutableArray<TypeSymbol> SubstituteTypesWithoutModifiers(ImmutableArray<TypeSymbol> original)
        {
            if (original.IsDefault)
            {
                return original;
            }

            TypeSymbol[] result = null;

            for (int i = 0; i < original.Length; i++)
            {
                var t = original[i];
                var substituted = SubstituteType(t).TypeSymbol;
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

        internal ImmutableArray<TypeSymbolWithAnnotations> SubstituteTypes(ImmutableArray<TypeSymbol> original)
        {
            if (original.IsDefault)
            {
                return default(ImmutableArray<TypeSymbolWithAnnotations>);
            }

            var result = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(original.Length);

            foreach (TypeSymbol t in original)
            {
                result.Add(SubstituteType(t));
            }

            return result.ToImmutableAndFree();
        }

        internal ImmutableArray<TypeSymbolWithAnnotations> SubstituteTypes(ImmutableArray<TypeSymbolWithAnnotations> original)
        {
            if (original.IsDefault)
            {
                return default(ImmutableArray<TypeSymbolWithAnnotations>);
            }

            var result = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(original.Length);

            foreach (TypeSymbolWithAnnotations t in original)
            {
                result.Add(SubstituteType(t));
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Substitute types, and return the results without duplicates, preserving the original order.
        /// </summary>
        internal void SubstituteConstraintTypesDistinct(ImmutableArray<TypeSymbolWithAnnotations> original, ArrayBuilder<TypeSymbolWithAnnotations> result)
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

                    // TODO: Do we need to merge annotations?
                    if (set.Add(substituted.TypeSymbol))
                    {
                        result.Add(substituted);
                    }
                }
            }
        }

        internal ImmutableArray<TypeParameterSymbol> SubstituteTypeParameters(ImmutableArray<TypeParameterSymbol> original)
        {
            return original.SelectAsArray((tp, m) => (TypeParameterSymbol)m.SubstituteTypeParameter(tp).AsTypeSymbolOnly(), this);
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
