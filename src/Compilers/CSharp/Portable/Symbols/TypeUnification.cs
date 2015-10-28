// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using System;

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
            if (t1 == t2)
            {
                return true;
            }

            MutableTypeMap substitution = null;
            bool result = CanUnifyHelper((object)t1 == null ? null : TypeSymbolWithAnnotations.Create(t1),
                                         (object)t2 == null ? null : TypeSymbolWithAnnotations.Create(t2), 
                                         ref substitution);
#if DEBUG
            if (result && ((object)t1 != null && (object)t2 != null))
            {
                var substituted1 = SubstituteAllTypeParameters(substitution, TypeSymbolWithAnnotations.Create(t1));
                var substituted2 = SubstituteAllTypeParameters(substitution, TypeSymbolWithAnnotations.Create(t2));

                Debug.Assert(substituted1.TypeSymbol == substituted2.TypeSymbol &&
                             substituted1.CustomModifiers.SequenceEqual(substituted2.CustomModifiers));
            }
#endif
            return result;
        }

#if DEBUG
        private static TypeSymbolWithAnnotations SubstituteAllTypeParameters(AbstractTypeMap substitution, TypeSymbolWithAnnotations type)
        {
            if (substitution != null)
            {
                TypeSymbolWithAnnotations previous;
                do
                {
                    previous = type;
                    type = type.SubstituteType(substitution);
                } while ((object)type != previous);
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
        private static bool CanUnifyHelper(TypeSymbolWithAnnotations t1, TypeSymbolWithAnnotations t2, ref MutableTypeMap substitution)
        {
            if ((object)t1 == null || (object)t2 == null)
            {
                return (object)t1 == t2;
            }

            if (t1.TypeSymbol == t2.TypeSymbol && t1.CustomModifiers.SequenceEqual(t2.CustomModifiers))
            {
                return true;
            }

            if (substitution != null)
            {
                t1 = t1.SubstituteType(substitution);
                t2 = t2.SubstituteType(substitution);
            }

            // If one of the types is a type parameter, then the substitution could make them equal.
            if (t1.TypeSymbol == t2.TypeSymbol && t1.CustomModifiers.SequenceEqual(t2.CustomModifiers))
            {
                return true;
            }

            // We can avoid a lot of redundant checks if we ensure that we only have to check
            // for type parameters on the LHS
            if (!t1.TypeSymbol.IsTypeParameter() && t2.TypeSymbol.IsTypeParameter())
            {
                TypeSymbolWithAnnotations tmp = t1;
                t1 = t2;
                t2 = tmp;
            }

            // If t1 is not a type parameter, then neither is t2
            Debug.Assert(t1.TypeSymbol.IsTypeParameter() || !t2.TypeSymbol.IsTypeParameter());

            switch (t1.Kind)
            {
                case SymbolKind.ArrayType:
                    {
                        if (t2.TypeKind != t1.TypeKind || !t2.CustomModifiers.SequenceEqual(t1.CustomModifiers))
                        {
                            return false;
                        }

                        ArrayTypeSymbol at1 = (ArrayTypeSymbol)t1.TypeSymbol;
                        ArrayTypeSymbol at2 = (ArrayTypeSymbol)t2.TypeSymbol;

                        if (!at1.HasSameShapeAs(at2))
                        {
                            return false;
                        }

                        return CanUnifyHelper(at1.ElementType, at2.ElementType, ref substitution);
                    }
                case SymbolKind.PointerType:
                    {
                        if (t2.TypeKind != t1.TypeKind || !t2.CustomModifiers.SequenceEqual(t1.CustomModifiers))
                        {
                            return false;
                        }

                        PointerTypeSymbol pt1 = (PointerTypeSymbol)t1.TypeSymbol;
                        PointerTypeSymbol pt2 = (PointerTypeSymbol)t2.TypeSymbol;

                        return CanUnifyHelper(pt1.PointedAtType, pt2.PointedAtType, ref substitution);
                    }
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    {
                        if (t2.TypeKind != t1.TypeKind || !t2.CustomModifiers.SequenceEqual(t1.CustomModifiers))
                        {
                            return false;
                        }

                        NamedTypeSymbol nt1 = (NamedTypeSymbol)t1.TypeSymbol;
                        NamedTypeSymbol nt2 = (NamedTypeSymbol)t2.TypeSymbol;

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

                        var nt1Arguments = nt1.TypeArgumentsNoUseSiteDiagnostics;
                        var nt2Arguments = nt2.TypeArgumentsNoUseSiteDiagnostics;

                        for (int i = 0; i < arity; i++)
                        {
                            if (!CanUnifyHelper(nt1Arguments[i], 
                                                nt2Arguments[i],  
                                                ref substitution))
                            {
                                return false;
                            }
                        }

                        // Note: Dev10 folds this into the loop since GetTypeArgsAll includes type args for containing types
                        // TODO: Calling CanUnifyHelper for the containing type is an overkill, we simply need to go through type arguments for all containers.
                        return (object)nt1.ContainingType == null || CanUnifyHelper(TypeSymbolWithAnnotations.Create(nt1.ContainingType), TypeSymbolWithAnnotations.Create(nt2.ContainingType), ref substitution);
                    }
                case SymbolKind.TypeParameter:
                    {
                        // These substitutions are not allowed in C#
                        if (t2.TypeKind == TypeKind.Pointer || t2.SpecialType == SpecialType.System_Void)
                        {
                            return false;
                        }

                        TypeParameterSymbol tp1 = (TypeParameterSymbol)t1.TypeSymbol;

                        // Perform the "occurs check" - i.e. ensure that t2 doesn't contain t1 to avoid recursive types
                        // Note: t2 can't be the same type param - we would have caught that with ReferenceEquals above
                        if (Contains(t2.TypeSymbol, tp1))
                        {
                            return false;
                        }

                        if (t1.CustomModifiers.IsDefaultOrEmpty)
                        {
                            AddSubstitution(ref substitution, tp1, t2);
                            return true;
                        }

                        if (t1.CustomModifiers.SequenceEqual(t2.CustomModifiers))
                        {
                            AddSubstitution(ref substitution, tp1, TypeSymbolWithAnnotations.Create(t2.TypeSymbol));
                            return true;
                        }

                        if (t1.CustomModifiers.Length < t2.CustomModifiers.Length &&
                            t1.CustomModifiers.SequenceEqual(t2.CustomModifiers.Take(t1.CustomModifiers.Length)))
                        {
                            AddSubstitution(ref substitution, tp1,
                                            TypeSymbolWithAnnotations.Create(t2.TypeSymbol,
                                                                  ImmutableArray.Create(t2.CustomModifiers, t1.CustomModifiers.Length, t2.CustomModifiers.Length - t1.CustomModifiers.Length)));
                            return true;
                        }

                        if (t2.TypeSymbol.IsTypeParameter())
                        {
                            var tp2 = (TypeParameterSymbol)t2.TypeSymbol;

                            if (t2.CustomModifiers.IsDefaultOrEmpty)
                            {
                                AddSubstitution(ref substitution, tp2, t1);
                                return true;
                            }

                            if (t2.CustomModifiers.Length < t1.CustomModifiers.Length &&
                                t2.CustomModifiers.SequenceEqual(t1.CustomModifiers.Take(t2.CustomModifiers.Length)))
                            {
                                AddSubstitution(ref substitution, tp2,
                                                TypeSymbolWithAnnotations.Create(t1.TypeSymbol,
                                                                      ImmutableArray.Create(t1.CustomModifiers, t2.CustomModifiers.Length, t1.CustomModifiers.Length - t2.CustomModifiers.Length)));
                                return true;
                            }
                        }

                        return false;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        private static void AddSubstitution(ref MutableTypeMap substitution, TypeParameterSymbol tp1, TypeSymbolWithAnnotations t2)
        {
            if (substitution == null)
            {
                substitution = new MutableTypeMap();
            }

            // MutableTypeMap.Add will throw if the key has already been added.  However,
            // if t1 was already in the substitution, it would have been substituted at the
            // start of CanUnifyHelper and we wouldn't be here.
            substitution.Add(tp1, t2);
        }

        /// <summary>
        /// Return true if the given type contains the specified type parameter.
        /// </summary>
        private static bool Contains(TypeSymbol type, TypeParameterSymbol typeParam)
        {
            switch (type.Kind)
            {
                case SymbolKind.ArrayType:
                    return Contains(((ArrayTypeSymbol)type).ElementType.TypeSymbol, typeParam);
                case SymbolKind.PointerType:
                    return Contains(((PointerTypeSymbol)type).PointedAtType.TypeSymbol, typeParam);
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    {
                        NamedTypeSymbol namedType = (NamedTypeSymbol)type;
                        while ((object)namedType != null)
                        {
                            foreach (TypeSymbolWithAnnotations typeArg in namedType.TypeArgumentsNoUseSiteDiagnostics)
                            {
                                if (Contains(typeArg.TypeSymbol, typeParam))
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
