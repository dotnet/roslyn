// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
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
                        return customModifierCountForTypeWithAnnotations(array.ElementTypeWithAnnotations);
                    }
                case SymbolKind.PointerType:
                    {
                        var pointer = (PointerTypeSymbol)type;
                        return customModifierCountForTypeWithAnnotations(pointer.PointedAtTypeWithAnnotations);
                    }
                case SymbolKind.FunctionPointerType:
                    {
                        return ((FunctionPointerTypeSymbol)type).Signature.CustomModifierCount();
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
                                ImmutableArray<TypeWithAnnotations> typeArgs = namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

                                foreach (TypeWithAnnotations typeArg in typeArgs)
                                {
                                    count += customModifierCountForTypeWithAnnotations(typeArg);
                                }

                                namedType = namedType.ContainingType;
                            }

                            return count;
                        }
                        break;
                    }
            }

            return 0;

            static int customModifierCountForTypeWithAnnotations(TypeWithAnnotations typeWithAnnotations)
                => typeWithAnnotations.CustomModifiers.Length + typeWithAnnotations.Type.CustomModifierCount();
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
                        TypeWithAnnotations elementType = array.ElementTypeWithAnnotations;
                        return checkTypeWithAnnotations(elementType, flagNonDefaultArraySizesOrLowerBounds)
                               || (flagNonDefaultArraySizesOrLowerBounds && !array.HasDefaultSizesAndLowerBounds);
                    }
                case SymbolKind.PointerType:
                    {
                        var pointer = (PointerTypeSymbol)type;
                        TypeWithAnnotations pointedAtType = pointer.PointedAtTypeWithAnnotations;
                        return checkTypeWithAnnotations(pointedAtType, flagNonDefaultArraySizesOrLowerBounds);
                    }
                case SymbolKind.FunctionPointerType:
                    {
                        var funcPtr = (FunctionPointerTypeSymbol)type;
                        if (!funcPtr.Signature.RefCustomModifiers.IsEmpty || checkTypeWithAnnotations(funcPtr.Signature.ReturnTypeWithAnnotations, flagNonDefaultArraySizesOrLowerBounds))
                        {
                            return true;
                        }

                        foreach (var param in funcPtr.Signature.Parameters)
                        {
                            if (!param.RefCustomModifiers.IsEmpty || checkTypeWithAnnotations(param.TypeWithAnnotations, flagNonDefaultArraySizesOrLowerBounds))
                            {
                                return true;
                            }
                        }

                        return false;
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
                                ImmutableArray<TypeWithAnnotations> typeArgs = namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

                                foreach (TypeWithAnnotations typeArg in typeArgs)
                                {
                                    if (checkTypeWithAnnotations(typeArg, flagNonDefaultArraySizesOrLowerBounds))
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

            static bool checkTypeWithAnnotations(TypeWithAnnotations typeWithAnnotations, bool flagNonDefaultArraySizesOrLowerBounds)
                => typeWithAnnotations.CustomModifiers.Any() || typeWithAnnotations.Type.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds);
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
        internal static TypeSymbol GetNextBaseTypeNoUseSiteDiagnostics(this TypeSymbol type, ConsList<TypeSymbol> basesBeingResolved, CSharpCompilation compilation, ref PooledHashSet<NamedTypeSymbol> visited)
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

                case TypeKind.Dynamic:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                case TypeKind.Array:
                case TypeKind.Submission:
                case TypeKind.Pointer:
                case TypeKind.FunctionPointer:
                case TypeKind.Extension:
                    // Enums, arrays, submissions, delegates and extensions know their own base types
                    // intrinsically (and do not include interface lists)
                    // so there is no possibility of a cycle.
                    return type.BaseTypeNoUseSiteDiagnostics;

                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private static TypeSymbol GetNextDeclaredBase(NamedTypeSymbol type, ConsList<TypeSymbol> basesBeingResolved, CSharpCompilation compilation, ref PooledHashSet<NamedTypeSymbol> visited)
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
                    return compilation.Assembly.GetSpecialType(SpecialType.System_Object);
                case TypeKind.Interface:
                    return null;
                case TypeKind.Struct:
                    return compilation.Assembly.GetSpecialType(SpecialType.System_ValueType);
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }
    }
}
