// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
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
                ImmutableArray<TypeWithAnnotations> oldFieldTypes = AnonymousTypeManager.GetAnonymousTypePropertyTypesWithAnnotations(previous);
                ImmutableArray<TypeWithAnnotations> newFieldTypes = SubstituteTypes(oldFieldTypes);
                return (oldFieldTypes == newFieldTypes) ? previous : AnonymousTypeManager.ConstructAnonymousTypeSymbol(previous, newFieldTypes);
            }

            if (previous.IsTupleType)
            {
                var previousTuple = (TupleTypeSymbol)previous;
                NamedTypeSymbol oldUnderlyingType = previousTuple.TupleUnderlyingType;
                NamedTypeSymbol newUnderlyingType = (NamedTypeSymbol)SubstituteType(oldUnderlyingType).Type;

                return ((object)newUnderlyingType == (object)oldUnderlyingType) ? previous : previousTuple.WithUnderlyingType(newUnderlyingType);
            }

            // TODO: we could construct the result's ConstructedFrom lazily by using a "deep"
            // construct operation here (as VB does), thereby avoiding alpha renaming in most cases.
            // Aleksey has shown that would reduce GC pressure if substitutions of deeply nested generics are common.
            NamedTypeSymbol oldConstructedFrom = previous.ConstructedFrom;
            NamedTypeSymbol newConstructedFrom = SubstituteMemberType(oldConstructedFrom);

            ImmutableArray<TypeWithAnnotations> oldTypeArguments = previous.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            bool changed = !ReferenceEquals(oldConstructedFrom, newConstructedFrom);
            var newTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance(oldTypeArguments.Length);

            for (int i = 0; i < oldTypeArguments.Length; i++)
            {
                var oldArgument = oldTypeArguments[i];
                var newArgument = oldArgument.SubstituteTypeWithTupleUnification(this);

                if (!changed && !oldArgument.IsSameAs(newArgument))
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
        internal TypeWithAnnotations SubstituteType(TypeSymbol previous)
        {
            if (ReferenceEquals(previous, null))
                return default(TypeWithAnnotations);

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

            return TypeWithAnnotations.Create(result);
        }

        internal TypeWithAnnotations SubstituteType(TypeWithAnnotations previous)
        {
            return previous.SubstituteType(this);
        }

        /// <summary>
        /// Same as <see cref="SubstituteType(TypeSymbol)"/>, but with special behavior around tuples.
        /// In particular, if substitution makes type tuple compatible, transform it into a tuple type.
        /// </summary>
        internal TypeWithAnnotations SubstituteType(TypeSymbol previous, bool withTupleUnification)
        {
            var result = SubstituteType(previous);
            // Make it a tuple if it became compatible with one.
            return withTupleUnification ? result.TransformToTupleIfCompatible() : result;
        }

        internal TypeWithAnnotations SubstituteTypeWithTupleUnification(TypeWithAnnotations previous)
        {
            return previous.SubstituteTypeWithTupleUnification(this);
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

                if (!TypeSymbol.Equals(modifier, substituted, TypeCompareKind.ConsiderEverything2))
                {
                    var builder = ArrayBuilder<CustomModifier>.GetInstance(customModifiers.Length);
                    builder.AddRange(customModifiers, i);

                    builder.Add(customModifiers[i].IsOptional ? CSharpCustomModifier.CreateOptional(substituted) : CSharpCustomModifier.CreateRequired(substituted));
                    for (i++; i < customModifiers.Length; i++)
                    {
                        modifier = (NamedTypeSymbol)customModifiers[i].Modifier;
                        substituted = SubstituteNamedType(modifier);

                        if (!TypeSymbol.Equals(modifier, substituted, TypeCompareKind.ConsiderEverything2))
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

        protected virtual TypeWithAnnotations SubstituteTypeParameter(TypeParameterSymbol typeParameter)
        {
            return TypeWithAnnotations.Create(typeParameter);
        }

        private ArrayTypeSymbol SubstituteArrayType(ArrayTypeSymbol t)
        {
            var oldElement = t.ElementTypeWithAnnotations;
            TypeWithAnnotations element = oldElement.SubstituteTypeWithTupleUnification(this);
            if (element.IsSameAs(oldElement))
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
            var oldPointedAtType = t.PointedAtTypeWithAnnotations;
            var pointedAtType = oldPointedAtType.SubstituteTypeWithTupleUnification(this);
            if (pointedAtType.IsSameAs(oldPointedAtType))
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
                var substituted = SubstituteType(t).Type;
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

        internal ImmutableArray<TypeWithAnnotations> SubstituteTypes(ImmutableArray<TypeSymbol> original)
        {
            if (original.IsDefault)
            {
                return default(ImmutableArray<TypeWithAnnotations>);
            }

            var result = ArrayBuilder<TypeWithAnnotations>.GetInstance(original.Length);

            foreach (TypeSymbol t in original)
            {
                result.Add(SubstituteType(t));
            }

            return result.ToImmutableAndFree();
        }

        internal ImmutableArray<TypeWithAnnotations> SubstituteTypes(ImmutableArray<TypeWithAnnotations> original)
        {
            if (original.IsDefault)
            {
                return default(ImmutableArray<TypeWithAnnotations>);
            }

            var result = ArrayBuilder<TypeWithAnnotations>.GetInstance(original.Length);

            foreach (TypeWithAnnotations t in original)
            {
                result.Add(SubstituteType(t));
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Substitute types, and return the results without duplicates, preserving the original order.
        /// Note, all occurrences of 'dynamic' in resulting types will be replaced with 'object'.
        /// </summary>
        internal void SubstituteConstraintTypesDistinctWithoutModifiers(
            TypeParameterSymbol owner,
            ImmutableArray<TypeWithAnnotations> original,
            ArrayBuilder<TypeWithAnnotations> result,
            HashSet<TypeParameterSymbol> ignoreTypesDependentOnTypeParametersOpt)
        {
            DynamicTypeEraser dynamicEraser = null;

            if (original.Length == 0)
            {
                return;
            }
            else if (original.Length == 1)
            {
                var type = original[0];
                if (ignoreTypesDependentOnTypeParametersOpt == null ||
                    !type.Type.ContainsTypeParameters(ignoreTypesDependentOnTypeParametersOpt))
                {
                    result.Add(substituteConstraintType(type));
                }
            }
            else
            {
                var map = PooledDictionary<TypeSymbol, int>.GetInstance();
                foreach (var type in original)
                {
                    if (ignoreTypesDependentOnTypeParametersOpt == null ||
                        !type.Type.ContainsTypeParameters(ignoreTypesDependentOnTypeParametersOpt))
                    {
                        var substituted = substituteConstraintType(type);

                        if (!map.TryGetValue(substituted.Type, out int mergeWith))
                        {
                            map.Add(substituted.Type, result.Count);
                            result.Add(substituted);
                        }
                        else
                        {
                            result[mergeWith] = ConstraintsHelper.ConstraintWithMostSignificantNullability(result[mergeWith], substituted);
                        }
                    }
                }

                map.Free();
            }

            TypeWithAnnotations substituteConstraintType(TypeWithAnnotations type)
            {
                if (dynamicEraser == null)
                {
                    dynamicEraser = new DynamicTypeEraser(owner.ContainingAssembly.CorLibrary.GetSpecialType(SpecialType.System_Object));
                }

                TypeWithAnnotations substituted = SubstituteType(type);

                return substituted.WithTypeAndModifiers(dynamicEraser.EraseDynamic(substituted.Type), substituted.CustomModifiers);
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
