// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class TypeUnification
    {
        /// <summary>
        /// Determine whether there is any substitution of type parameters that will
        /// make two types identical.
        /// </summary>
        public static bool CanUnify(TypeSymbol t1, TypeSymbol t2)
        {
            MutableTypeMap substitution = null;
            bool result = CanUnifyHelper(t1, t2, ref substitution);
#if DEBUG
            Debug.Assert(!result ||
                SubstituteAllTypeParameters(substitution, t1) == SubstituteAllTypeParameters(substitution, t2));
#endif
            return result;
        }

#if DEBUG
        private static TypeSymbol SubstituteAllTypeParameters(AbstractTypeMap substitution, TypeSymbol type)
        {
            if (substitution != null)
            {
                TypeSymbol previous;
                do
                {
                    previous = type;
                    type = substitution.SubstituteType(type);
                } while (type != previous);
            }
            return type;
        }
#endif

        /// <summary>
        /// Determine whether there is any substitution of type parameters that will
        /// make two types identical.
        /// </summary>
        /// <param name="t1">LHS</param>
        /// <param name="t2">RHS</param>
        /// <param name="substitution">
        /// Substitutions performed so far (or null for none).
        /// Keys are type parameters, values are types (possibly type parameters).
        /// Will be updated with new substitutions by the callee.
        /// Should be ignored when false is returned.
        /// </param>
        /// <returns>True if there exists a type map such that Map(LHS) == Map(RHS).</returns>
        /// <remarks>
        /// Derived from Dev10's BSYMMGR::UnifyTypes.
        /// Two types will not unify if they have different custom modifiers.
        /// </remarks>
        private static bool CanUnifyHelper(TypeSymbol t1, TypeSymbol t2, ref MutableTypeMap substitution)
        {
            if (ReferenceEquals(t1, t2))
            {
                return true;
            }
            else if ((object)t1 == null || (object)t2 == null)
            {
                // Can't both be null or they would have been ReferenceEquals
                return false;
            }

            if (substitution != null)
            {
                t1 = substitution.SubstituteType(t1);
                t2 = substitution.SubstituteType(t2);
            }

            // If one of the types is a type parameter, then the substitution could make them ReferenceEquals.
            if (ReferenceEquals(t1, t2))
            {
                return true;
            }

            // We can avoid a lot of redundant checks if we ensure that we only have to check
            // for type parameters on the LHS
            if (!t1.IsTypeParameter() && t2.IsTypeParameter())
            {
                TypeSymbol tmp = t1;
                t1 = t2;
                t2 = tmp;
            }

            // If t1 is not a type parameter, then neither is t2
            Debug.Assert(t1.IsTypeParameter() || !t2.IsTypeParameter());

            switch (t1.Kind)
            {
                case SymbolKind.ArrayType:
                    {
                        if (t2.TypeKind != t1.TypeKind)
                        {
                            return false;
                        }

                        ArrayTypeSymbol at1 = (ArrayTypeSymbol)t1;
                        ArrayTypeSymbol at2 = (ArrayTypeSymbol)t2;

                        if (at1.Rank != at2.Rank || !at1.CustomModifiers.SequenceEqual(at2.CustomModifiers))
                        {
                            return false;
                        }

                        return CanUnifyHelper(at1.ElementType, at2.ElementType, ref substitution);
                    }
                case SymbolKind.PointerType:
                    {
                        if (t2.TypeKind != t1.TypeKind)
                        {
                            return false;
                        }

                        PointerTypeSymbol pt1 = (PointerTypeSymbol)t1;
                        PointerTypeSymbol pt2 = (PointerTypeSymbol)t2;

                        if (!pt1.CustomModifiers.SequenceEqual(pt2.CustomModifiers))
                        {
                            return false;
                        }

                        return CanUnifyHelper(pt1.PointedAtType, pt2.PointedAtType, ref substitution);
                    }
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    {
                        if (t2.TypeKind != t1.TypeKind)
                        {
                            return false;
                        }

                        NamedTypeSymbol nt1 = (NamedTypeSymbol)t1;
                        NamedTypeSymbol nt2 = (NamedTypeSymbol)t2;

                        if (!nt1.IsGenericType)
                        {
                            return !nt2.IsGenericType && nt1 == nt2;
                        }
                        else if (!nt2.IsGenericType)
                        {
                            return false;
                        }

                        int arity = nt1.Arity;

                        if (nt2.Arity != arity || nt2.OriginalDefinition != nt1.OriginalDefinition)
                        {
                            return false;
                        }

                        for (int i = 0; i < arity; i++)
                        {
                            if (!CanUnifyHelper(nt1.TypeArgumentsNoUseSiteDiagnostics[i], nt2.TypeArgumentsNoUseSiteDiagnostics[i], ref substitution))
                            {
                                return false;
                            }
                        }

                        // Note: Dev10 folds this into the loop since GetTypeArgsAll includes type args for containing types
                        return (object)nt1.ContainingType == null || CanUnifyHelper(nt1.ContainingType, nt2.ContainingType, ref substitution);
                    }
                case SymbolKind.TypeParameter:
                    {
                        // These substitutions are not allowed in C#
                        if (t2.TypeKind == TypeKind.Pointer || t2.SpecialType == SpecialType.System_Void)
                        {
                            return false;
                        }

                        TypeParameterSymbol tp1 = (TypeParameterSymbol)t1;

                        // Perform the "occurs check" - i.e. ensure that t2 doesn't contain t1 to avoid recursive types
                        // Note: t2 can't be the same type param - we would have caught that with ReferenceEquals above
                        if (Contains(t2, tp1))
                        {
                            return false;
                        }

                        if (substitution == null)
                        {
                            substitution = new MutableTypeMap();
                        }

                        // MutableTypeMap.Add will throw if the key has already been added.  However,
                        // if t1 was already in the substitution, it would have been substituted at the
                        // start of this method and we wouldn't be here.
                        substitution.Add(tp1, t2);

                        return true;
                    }
                default:
                    {
                        return t1 == t2;
                    }
            }
        }

        /// <summary>
        /// Return true if the given type contains the specified type parameter.
        /// </summary>
        private static bool Contains(TypeSymbol type, TypeParameterSymbol typeParam)
        {
            switch (type.Kind)
            {
                case SymbolKind.ArrayType:
                    return Contains(((ArrayTypeSymbol)type).ElementType, typeParam);
                case SymbolKind.PointerType:
                    return Contains(((PointerTypeSymbol)type).PointedAtType, typeParam);
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    {
                        NamedTypeSymbol namedType = (NamedTypeSymbol)type;
                        while ((object)namedType != null)
                        {
                            foreach (TypeSymbol typeArg in namedType.TypeArgumentsNoUseSiteDiagnostics)
                            {
                                if (Contains(typeArg, typeParam))
                                {
                                    return true;
                                }
                            }
                            namedType = namedType.ContainingType;
                        }

                        return false;
                    }
                case SymbolKind.TypeParameter:
                    return type == typeParam;
                default:
                    return false;
            }
        }
    }
}
