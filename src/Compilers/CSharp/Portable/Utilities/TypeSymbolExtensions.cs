// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class TypeSymbolExtensions
    {
        /// <summary>
        /// Count the custom modifiers within the specified TypeSymbol.
        /// Potentially non-zero for arrays, pointers, and generic instantiations.
        /// </summary>
        public static int CustomModifierCount(this TypeSymbol type)
        {
            if ((object)type == null)
            {
                return 0;
            }

            // Custom modifiers can be inside arrays, pointers and generic instantiations
            switch (type.Kind)
            {
                case SymbolKind.ArrayType:
                    {
                        var array = (ArrayTypeSymbol)type;
                        TypeSymbolWithAnnotations elementType = array.ElementType;
                        return elementType.CustomModifiers.Length + elementType.TypeSymbol.CustomModifierCount();
                    }
                case SymbolKind.PointerType:
                    {
                        var pointer = (PointerTypeSymbol)type;
                        TypeSymbolWithAnnotations pointedAtType = pointer.PointedAtType;
                        return pointedAtType.CustomModifiers.Length + pointedAtType.TypeSymbol.CustomModifierCount();
                    }
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    {
                        bool isDefinition = type.IsDefinition;

                        if (!isDefinition)
                        {
                            var namedType = (NamedTypeSymbol)type;
                            int count = 0;

                            while ((object)namedType != null)
                            {
                                ImmutableArray<TypeSymbolWithAnnotations> typeArgs = namedType.TypeArgumentsNoUseSiteDiagnostics;

                                foreach (TypeSymbolWithAnnotations typeArg in typeArgs)
                                {
                                    count += typeArg.TypeSymbol.CustomModifierCount() + typeArg.CustomModifiers.Length;
                                }

                                namedType = namedType.ContainingType;
                            }

                            return count;
                        }
                        break;
                    }
            }

            return 0;
        }

        /// <summary>
        /// Check for custom modifiers within the specified TypeSymbol.
        /// Potentially true for arrays, pointers, and generic instantiations.
        /// </summary>
        /// <remarks>
        /// A much less efficient implementation would be CustomModifierCount() == 0.
        /// CONSIDER: Could share a backing method with CustomModifierCount.
        /// </remarks>
        public static bool HasCustomModifiers(this TypeSymbol type, bool flagNonDefaultArraySizesOrLowerBounds)
        {
            if ((object)type == null)
            {
                return false;
            }

            // Custom modifiers can be inside arrays, pointers and generic instantiations
            switch (type.Kind)
            {
                case SymbolKind.ArrayType:
                    {
                        var array = (ArrayTypeSymbol)type;
                        TypeSymbolWithAnnotations elementType = array.ElementType;
                        return elementType.CustomModifiers.Any() || elementType.TypeSymbol.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds) || 
                               (flagNonDefaultArraySizesOrLowerBounds && !array.HasDefaultSizesAndLowerBounds);
                    }
                case SymbolKind.PointerType:
                    {
                        var pointer = (PointerTypeSymbol)type;
                        TypeSymbolWithAnnotations pointedAtType = pointer.PointedAtType;
                        return pointedAtType.CustomModifiers.Any() || pointedAtType.TypeSymbol.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds);
                    }
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    {
                        bool isDefinition = type.IsDefinition;

                        if (!isDefinition)
                        {
                            var namedType = (NamedTypeSymbol)type;
                            while ((object)namedType != null)
                            {
                                ImmutableArray<TypeSymbolWithAnnotations> typeArgs = namedType.TypeArgumentsNoUseSiteDiagnostics;

                                foreach (TypeSymbolWithAnnotations typeArg in typeArgs)
                                {
                                    if (!typeArg.CustomModifiers.IsEmpty || typeArg.TypeSymbol.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds))
                                    {
                                        return true;
                                    }
                                }

                                namedType = namedType.ContainingType;
                            }
                        }
                        break;
                    }
            }

            return false;
        }

        /// <summary>
        /// Return true if this type can unify with the specified type
        /// (i.e. is the same for some substitution of type parameters).
        /// </summary>
        public static bool CanUnifyWith(this TypeSymbol thisType, TypeSymbol otherType)
        {
            return TypeUnification.CanUnify(thisType, otherType);
        }

        /// <summary>
        /// Used when iterating through base types in contexts in which the caller needs to avoid cycles and can't use BaseType
        /// (perhaps because BaseType is in the process of being computed)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="basesBeingResolved"></param>
        /// <param name="compilation"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        internal static TypeSymbol GetNextBaseTypeNoUseSiteDiagnostics(this TypeSymbol type, ConsList<Symbol> basesBeingResolved, CSharpCompilation compilation, ref PooledHashSet<NamedTypeSymbol> visited)
        {
            switch (type.TypeKind)
            {
                case TypeKind.TypeParameter:
                    return ((TypeParameterSymbol)type).EffectiveBaseClassNoUseSiteDiagnostics;

                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Error:
                case TypeKind.Interface:
                    return GetNextDeclaredBase((NamedTypeSymbol)type, basesBeingResolved, compilation, ref visited);

                default:
                    // Enums and delegates know their own base types
                    // intrinsically (and do not include interface lists)
                    // so there is not the possibility of a cycle.
                    return type.BaseTypeNoUseSiteDiagnostics;
            }
        }

        private static TypeSymbol GetNextDeclaredBase(NamedTypeSymbol type, ConsList<Symbol> basesBeingResolved, CSharpCompilation compilation, ref PooledHashSet<NamedTypeSymbol> visited)
        {
            // We shouldn't have visited this type earlier.
            Debug.Assert(visited == null || !visited.Contains(type.OriginalDefinition));

            if (basesBeingResolved != null && basesBeingResolved.ContainsReference(type.OriginalDefinition))
            {
                return null;
            }

            if (type.SpecialType == SpecialType.System_Object)
            {
                type.SetKnownToHaveNoDeclaredBaseCycles();
                return null;
            }

            var nextType = type.GetDeclaredBaseType(basesBeingResolved);

            // types with no declared bases inherit object's members
            if ((object)nextType == null)
            {
                SetKnownToHaveNoDeclaredBaseCycles(ref visited);
                return GetDefaultBaseOrNull(type, compilation);
            }

            var origType = type.OriginalDefinition;
            if (nextType.KnownToHaveNoDeclaredBaseCycles)
            {
                origType.SetKnownToHaveNoDeclaredBaseCycles();
                SetKnownToHaveNoDeclaredBaseCycles(ref visited);
            }
            else
            {
                // start cycle tracking
                visited = visited ?? PooledHashSet<NamedTypeSymbol>.GetInstance();
                visited.Add(origType);
                if (visited.Contains(nextType.OriginalDefinition))
                {
                    return GetDefaultBaseOrNull(type, compilation);
                }
            }

            return nextType;
        }

        private static void SetKnownToHaveNoDeclaredBaseCycles(ref PooledHashSet<NamedTypeSymbol> visited)
        {
            if (visited != null)
            {
                foreach (var v in visited)
                {
                    v.SetKnownToHaveNoDeclaredBaseCycles();
                }

                visited.Free();
                visited = null;
            }
        }

        private static NamedTypeSymbol GetDefaultBaseOrNull(NamedTypeSymbol type, CSharpCompilation compilation)
        {
            if (compilation == null)
            {
                return null;
            }

            switch (type.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Error:
                case TypeKind.Interface:
                    return compilation.Assembly.GetSpecialType(SpecialType.System_Object);
                case TypeKind.Struct:
                    return compilation.Assembly.GetSpecialType(SpecialType.System_ValueType);
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }
    }
}
