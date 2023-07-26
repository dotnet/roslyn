﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
            Debug.Assert(t1 is not null);
            Debug.Assert(t2 is not null);

            if (TypeSymbol.Equals(t1, t2, TypeCompareKind.CLRSignatureCompareOptions))
            {
                return true;
            }

            MutableTypeMap? substitution = null;
            bool result = CanUnifyHelper(t1, t2, onlySubstituteInLHS: false, ref substitution);
#if DEBUG
            if (result)
            {
                var substituted1 = SubstituteAllTypeParameters(substitution, TypeWithAnnotations.Create(t1));
                var substituted2 = SubstituteAllTypeParameters(substitution, TypeWithAnnotations.Create(t2));

                Debug.Assert(substituted1.Type.Equals(substituted2.Type, TypeCompareKind.CLRSignatureCompareOptions));
                Debug.Assert(substituted1.CustomModifiers.SequenceEqual(substituted2.CustomModifiers));
            }
#endif
            return result;
        }

        /// <summary>
        /// Determines a substitution of type parameters on <paramref name="extension"/>
        /// that yields <paramref name="type"/>.
        /// The substitution should not touch any of the containing type's type parameters
        /// and it should substitute all of the extension's own type parameters.
        /// </summary>
        public static bool CanImplicitlyExtend(NamedTypeSymbol extension, TypeSymbol type, out AbstractTypeParameterMap? map)
        {
            Debug.Assert(extension is not null);
            Debug.Assert(type is not null);

            var extensionUnderlyingType = extension.ExtendedTypeNoUseSiteDiagnostics;
            Debug.Assert(extensionUnderlyingType is not null);

            // PROTOTYPE we'll want to adjust the handling for differences that aren't relevant to the CLR, such as object/dynamic
            if (TypeSymbol.Equals(extensionUnderlyingType, type, TypeCompareKind.CLRSignatureCompareOptions))
            {
                map = null;
                return true;
            }

            MutableTypeMap? substitution = null;
            bool result = CanUnifyHelper(extensionUnderlyingType, type, onlySubstituteInLHS: true, ref substitution);
#if DEBUG
            if (result && (extensionUnderlyingType is not null && type is not null))
            {
                var substitutedUnderlyingType = SubstituteAllTypeParameters(substitution, TypeWithAnnotations.Create(extensionUnderlyingType));
                Debug.Assert(substitutedUnderlyingType.Type.Equals(type, TypeCompareKind.CLRSignatureCompareOptions));
            }
#endif

            // In error scenarios where we end up with unsubstituted type parameters,
            // we reject the extension.
            foreach (var typeParameter in extension.TypeParameters)
            {
                if (substitution is null || !substitution.Contains(typeParameter))
                {
                    map = null;
                    return false;
                }
            }

            // We cannot allow any of the type parameters of the extension's containing type to be substituted
            if (hasSubstitutionForContainingTypeTypeParameter(extension.ContainingType, substitution))
            {
                map = null;
                return false;
            }

            map = substitution;
            return result;

            static bool hasSubstitutionForContainingTypeTypeParameter(NamedTypeSymbol? containingType, MutableTypeMap? substitution)
            {
                if (substitution is null)
                {
                    return false;
                }

                while (containingType is not null)
                {
                    foreach (var typeParameter in containingType.TypeParameters)
                    {
                        if (substitution.Contains(typeParameter))
                        {
                            return true;
                        }
                    }

                    containingType = containingType.ContainingType;
                }

                return false;
            }
        }

#if DEBUG
        private static TypeWithAnnotations SubstituteAllTypeParameters(AbstractTypeMap? substitution, TypeWithAnnotations type)
        {
            if (substitution != null)
            {
                TypeWithAnnotations previous;
                do
                {
                    previous = type;
                    type = type.SubstituteType(substitution);
                } while (!type.IsSameAs(previous));
            }

            return type;
        }
#endif

        private static bool CanUnifyHelper(TypeSymbol t1, TypeSymbol t2, bool onlySubstituteInLHS, ref MutableTypeMap? substitution)
        {
            return CanUnifyHelper(TypeWithAnnotations.Create(t1), TypeWithAnnotations.Create(t2), onlySubstituteInLHS, ref substitution);
        }

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
        private static bool CanUnifyHelper(TypeWithAnnotations t1, TypeWithAnnotations t2, bool onlySubstituteInLHS, ref MutableTypeMap? substitution)
        {
            if (!t1.HasType || !t2.HasType)
            {
                return t1.IsSameAs(t2);
            }

            if (substitution != null)
            {
                t1 = t1.SubstituteType(substitution);
                if (!onlySubstituteInLHS)
                {
                    t2 = t2.SubstituteType(substitution);
                }
            }

            if (TypeSymbol.Equals(t1.Type, t2.Type, TypeCompareKind.CLRSignatureCompareOptions) && t1.CustomModifiers.SequenceEqual(t2.CustomModifiers))
            {
                return true;
            }

            if (!onlySubstituteInLHS)
            {
                // We can avoid a lot of redundant checks if we ensure that we only have to check
                // for type parameters on the LHS
                if (!t1.Type.IsTypeParameter() && t2.Type.IsTypeParameter())
                {
                    TypeWithAnnotations tmp = t1;
                    t1 = t2;
                    t2 = tmp;
                }

                // If t1 is not a type parameter, then neither is t2
                Debug.Assert(t1.Type.IsTypeParameter() || !t2.Type.IsTypeParameter());
            }

            switch (t1.Type.Kind)
            {
                case SymbolKind.ArrayType:
                    {
                        if (t2.TypeKind != t1.TypeKind || !t2.CustomModifiers.SequenceEqual(t1.CustomModifiers))
                        {
                            return false;
                        }

                        ArrayTypeSymbol at1 = (ArrayTypeSymbol)t1.Type;
                        ArrayTypeSymbol at2 = (ArrayTypeSymbol)t2.Type;

                        if (!at1.HasSameShapeAs(at2))
                        {
                            return false;
                        }

                        return CanUnifyHelper(at1.ElementTypeWithAnnotations, at2.ElementTypeWithAnnotations, onlySubstituteInLHS, ref substitution);
                    }
                case SymbolKind.PointerType:
                    {
                        if (t2.TypeKind != t1.TypeKind || !t2.CustomModifiers.SequenceEqual(t1.CustomModifiers))
                        {
                            return false;
                        }

                        PointerTypeSymbol pt1 = (PointerTypeSymbol)t1.Type;
                        PointerTypeSymbol pt2 = (PointerTypeSymbol)t2.Type;

                        return CanUnifyHelper(pt1.PointedAtTypeWithAnnotations, pt2.PointedAtTypeWithAnnotations, onlySubstituteInLHS, ref substitution);
                    }
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    {
                        if (t2.TypeKind != t1.TypeKind || !t2.CustomModifiers.SequenceEqual(t1.CustomModifiers))
                        {
                            return false;
                        }

                        NamedTypeSymbol nt1 = (NamedTypeSymbol)t1.Type;
                        NamedTypeSymbol nt2 = (NamedTypeSymbol)t2.Type;
                        if (!nt1.IsGenericType || !nt2.IsGenericType)
                        {
                            // Initial TypeSymbol.Equals(...) && CustomModifiers.SequenceEqual(...) failed above,
                            // and custom modifiers compared equal in this case block, so the types must be distinct.
                            Debug.Assert(!nt1.Equals(nt2, TypeCompareKind.CLRSignatureCompareOptions));
                            return false;
                        }

                        int arity = nt1.Arity;

                        if (nt2.Arity != arity || !TypeSymbol.Equals(nt2.OriginalDefinition, nt1.OriginalDefinition, TypeCompareKind.ConsiderEverything))
                        {
                            return false;
                        }

                        var nt1Arguments = nt1.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
                        var nt2Arguments = nt2.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

                        for (int i = 0; i < arity; i++)
                        {
                            if (!CanUnifyHelper(nt1Arguments[i], nt2Arguments[i], onlySubstituteInLHS, ref substitution))
                            {
                                return false;
                            }
                        }

                        // Note: Dev10 folds this into the loop since GetTypeArgsAll includes type args for containing types
                        // TODO: Calling CanUnifyHelper for the containing type is an overkill, we simply need to go through type arguments for all containers.
                        return (object)nt1.ContainingType == null || CanUnifyHelper(nt1.ContainingType, nt2.ContainingType, onlySubstituteInLHS, ref substitution);
                    }
                case SymbolKind.TypeParameter:
                    {
                        // These substitutions are not allowed in C#
                        if (t2.Type.IsPointerOrFunctionPointer() || t2.IsVoidType())
                        {
                            return false;
                        }

                        TypeParameterSymbol tp1 = (TypeParameterSymbol)t1.Type;

                        // Perform the "occurs check" - i.e. ensure that t2 doesn't contain t1 to avoid recursive types
                        // Note: t2 can't be the same type param - we would have caught that with ReferenceEquals above
                        if (t2.Type.ContainsTypeParameter(tp1))
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
                            AddSubstitution(ref substitution, tp1, TypeWithAnnotations.Create(t2.Type));
                            return true;
                        }

                        if (t1.CustomModifiers.Length < t2.CustomModifiers.Length &&
                            t1.CustomModifiers.SequenceEqual(t2.CustomModifiers.Take(t1.CustomModifiers.Length)))
                        {
                            AddSubstitution(ref substitution, tp1,
                                TypeWithAnnotations.Create(t2.Type,
                                    customModifiers: ImmutableArray.Create(t2.CustomModifiers, t1.CustomModifiers.Length, t2.CustomModifiers.Length - t1.CustomModifiers.Length)));
                            return true;
                        }

                        if (!onlySubstituteInLHS && t2.Type.IsTypeParameter())
                        {
                            var tp2 = (TypeParameterSymbol)t2.Type;

                            if (t2.CustomModifiers.IsDefaultOrEmpty)
                            {
                                AddSubstitution(ref substitution, tp2, t1);
                                return true;
                            }

                            if (t2.CustomModifiers.Length < t1.CustomModifiers.Length &&
                                t2.CustomModifiers.SequenceEqual(t1.CustomModifiers.Take(t2.CustomModifiers.Length)))
                            {
                                AddSubstitution(ref substitution, tp2,
                                    TypeWithAnnotations.Create(t1.Type,
                                        customModifiers: ImmutableArray.Create(t1.CustomModifiers, t2.CustomModifiers.Length, t1.CustomModifiers.Length - t2.CustomModifiers.Length)));
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

        private static void AddSubstitution(ref MutableTypeMap? substitution, TypeParameterSymbol tp1, TypeWithAnnotations t2)
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
    }
}
