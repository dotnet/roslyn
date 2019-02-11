﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class TypeSymbolExtensions
    {
        public static bool ImplementsInterface(this TypeSymbol subType, TypeSymbol superInterface, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            foreach (NamedTypeSymbol @interface in subType.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (@interface.IsInterface && TypeSymbol.Equals(@interface, superInterface, TypeCompareKind.ConsiderEverything2))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CanBeAssignedNull(this TypeSymbol type)
        {
            return type.IsReferenceType || type.IsPointerType() || type.IsNullableType();
        }

        public static bool CanContainNull(this TypeSymbol type)
        {
            // unbound type parameters might contain null, even though they cannot be *assigned* null.
            return !type.IsValueType || type.IsNullableType();
        }

        public static bool CanBeConst(this TypeSymbol typeSymbol)
        {
            Debug.Assert((object)typeSymbol != null);

            return typeSymbol.IsReferenceType || typeSymbol.IsEnumType() || typeSymbol.SpecialType.CanBeConst();
        }

        /// <summary>
        /// T => true
        /// T where T : struct => false
        /// T where T : class => false
        /// T where T : class? => true
        /// T where T : IComparable => true
        /// T where T : IComparable? => true
        /// </summary>
        public static bool IsTypeParameterDisallowingAnnotation(this TypeSymbol type)
        {
            if (type.TypeKind != TypeKind.TypeParameter)
            {
                return false;
            }
            var typeParameter = (TypeParameterSymbol)type;
            // https://github.com/dotnet/roslyn/issues/30056: Test `where T : unmanaged`. See
            // UninitializedNonNullableFieldTests.TypeParameterConstraints for instance.
            return !typeParameter.IsValueType && !(typeParameter.IsReferenceType && typeParameter.IsNotNullableIfReferenceType == true);
        }

        /// <summary>
        /// T => true
        /// T where T : struct => false
        /// T where T : class => false
        /// T where T : class? => true
        /// T where T : IComparable => false
        /// T where T : IComparable? => true
        /// </summary>
        public static bool IsPossiblyNullableReferenceTypeTypeParameter(this TypeSymbol type)
        {
            if (type.TypeKind != TypeKind.TypeParameter)
            {
                return false;
            }
            var typeParameter = (TypeParameterSymbol)type;
            return !typeParameter.IsValueType && typeParameter.IsNotNullableIfReferenceType != true;
        }

        public static bool IsNonNullableValueType(this TypeSymbol typeArgument)
        {
            if (!typeArgument.IsValueType)
            {
                return false;
            }

            return !IsNullableTypeOrTypeParameter(typeArgument);
        }

        public static bool IsNullableTypeOrTypeParameter(this TypeSymbol type)
        {
            if (type.TypeKind == TypeKind.TypeParameter)
            {
                var constraintTypes = ((TypeParameterSymbol)type).ConstraintTypesNoUseSiteDiagnostics;
                foreach (var constraintType in constraintTypes)
                {
                    if (constraintType.IsNullableTypeOrTypeParameter())
                    {
                        return true;
                    }
                }
                return false;
            }

            return type.IsNullableType();
        }

        public static bool IsNullableType(this TypeSymbol type)
        {
            return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        public static TypeSymbol GetNullableUnderlyingType(this TypeSymbol type)
        {
            return type.GetNullableUnderlyingTypeWithAnnotations().TypeSymbol;
        }

        public static TypeSymbolWithAnnotations GetNullableUnderlyingTypeWithAnnotations(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(IsNullableType(type));
            Debug.Assert(type is NamedTypeSymbol);  //not testing Kind because it may be an ErrorType

            return ((NamedTypeSymbol)type).TypeArgumentsNoUseSiteDiagnostics[0];
        }

        public static TypeSymbol StrippedType(this TypeSymbol type)
        {
            return type.IsNullableType() ? type.GetNullableUnderlyingType() : type;
        }

        public static TypeSymbol TupleUnderlyingTypeOrSelf(this TypeSymbol type)
        {
            return type.TupleUnderlyingType ?? type;
        }

        public static TypeSymbol EnumUnderlyingType(this TypeSymbol type)
        {
            return type.IsEnumType() ? type.GetEnumUnderlyingType() : type;
        }

        public static bool IsObjectType(this TypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Object;
        }

        public static bool IsStringType(this TypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_String;
        }

        public static bool IsCharType(this TypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Char;
        }

        public static bool IsIntegralType(this TypeSymbol type)
        {
            return type.SpecialType.IsIntegralType();
        }

        public static NamedTypeSymbol GetEnumUnderlyingType(this TypeSymbol type)
        {
            var namedType = type as NamedTypeSymbol;
            return ((object)namedType != null) ? namedType.EnumUnderlyingType : null;
        }

        public static bool IsEnumType(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.TypeKind == TypeKind.Enum;
        }

        public static bool IsValidEnumType(this TypeSymbol type)
        {
            var underlyingType = type.GetEnumUnderlyingType();
            // SpecialType will be None if the underlying type is invalid.
            return ((object)underlyingType != null) && (underlyingType.SpecialType != SpecialType.None);
        }

        /// <summary>
        /// Determines if the given type is a valid attribute parameter type.
        /// </summary>
        /// <param name="type">Type to validated</param>
        /// <param name="compilation">compilation</param>
        /// <returns></returns>
        public static bool IsValidAttributeParameterType(this TypeSymbol type, CSharpCompilation compilation)
        {
            return GetAttributeParameterTypedConstantKind(type, compilation) != TypedConstantKind.Error;
        }

        /// <summary>
        /// Gets the typed constant kind for the given attribute parameter type.
        /// </summary>
        /// <param name="type">Type to validated</param>
        /// <param name="compilation">compilation</param>
        /// <returns>TypedConstantKind for the attribute parameter type.</returns>
        public static TypedConstantKind GetAttributeParameterTypedConstantKind(this TypeSymbol type, CSharpCompilation compilation)
        {
            // Spec (17.1.3)
            // The types of positional and named parameters for an attribute class are limited to the attribute parameter types, which are:
            // 	1) One of the following types: bool, byte, char, double, float, int, long, sbyte, short, string, uint, ulong, ushort.
            //     2) The type object.
            //     3) The type System.Type.
            //     4) An enum type, provided it has public accessibility and the types in which it is nested (if any) also have public accessibility.
            //     5) Single-dimensional arrays of the above types.
            // A constructor argument or public field which does not have one of these types, cannot be used as a positional
            // or named parameter in an attribute specification.

            TypedConstantKind kind = TypedConstantKind.Error;
            if ((object)type == null)
            {
                return TypedConstantKind.Error;
            }

            if (type.Kind == SymbolKind.ArrayType)
            {
                var arrayType = (ArrayTypeSymbol)type;
                if (!arrayType.IsSZArray)
                {
                    return TypedConstantKind.Error;
                }

                kind = TypedConstantKind.Array;
                type = arrayType.ElementType.TypeSymbol;
            }

            // enum or enum[]
            if (type.IsEnumType())
            {
                // SPEC VIOLATION: Dev11 doesn't enforce either the Enum type or its enclosing types (if any) to have public accessibility.
                // We will be consistent with Dev11 behavior.

                if (kind == TypedConstantKind.Error)
                {
                    // set only if kind is not already set (i.e. its not an array of enum)
                    kind = TypedConstantKind.Enum;
                }

                type = type.GetEnumUnderlyingType();
            }

            var typedConstantKind = TypedConstant.GetTypedConstantKind(type, compilation);
            switch (typedConstantKind)
            {
                case TypedConstantKind.Array:
                case TypedConstantKind.Enum:
                case TypedConstantKind.Error:
                    return TypedConstantKind.Error;

                default:
                    if (kind == TypedConstantKind.Array || kind == TypedConstantKind.Enum)
                    {
                        // Array/Enum type with valid element/underlying type
                        return kind;
                    }

                    return typedConstantKind;
            }
        }

        public static bool IsValidExtensionParameterType(this TypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Pointer:
                case TypeKind.Dynamic:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsInterfaceType(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)type).IsInterface;
        }

        public static bool IsClassType(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.TypeKind == TypeKind.Class;
        }

        public static bool IsStructType(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.TypeKind == TypeKind.Struct;
        }

        public static bool IsErrorType(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.Kind == SymbolKind.ErrorType;
        }

        public static bool IsMethodTypeParameter(this TypeParameterSymbol p)
        {
            return p.ContainingSymbol.Kind == SymbolKind.Method;
        }

        public static bool IsDynamic(this TypeSymbol type)
        {
            return type.TypeKind == TypeKind.Dynamic;
        }

        public static bool IsTypeParameter(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.TypeKind == TypeKind.TypeParameter;
        }

        public static bool IsArray(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.TypeKind == TypeKind.Array;
        }

        public static bool IsSZArray(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.TypeKind == TypeKind.Array && ((ArrayTypeSymbol)type).IsSZArray;
        }

        // If the type is a delegate type, it returns it. If the type is an
        // expression tree type associated with a delegate type, it returns
        // the delegate type. Otherwise, null.
        public static NamedTypeSymbol GetDelegateType(this TypeSymbol type)
        {
            if ((object)type == null) return null;
            if (type.IsExpressionTree())
            {
                type = ((NamedTypeSymbol)type).TypeArgumentsNoUseSiteDiagnostics[0].TypeSymbol;
            }

            return type.IsDelegateType() ? (NamedTypeSymbol)type : null;
        }

        /// <summary>
        /// return true if the type is constructed from System.Linq.Expressions.Expression`1
        /// </summary>
        public static bool IsExpressionTree(this TypeSymbol _type)
        {
            return _type.OriginalDefinition is NamedTypeSymbol type &&
                type.Arity == 1 &&
                type.MangleName &&
                type.Name == "Expression" &&
                CheckFullName(type.ContainingSymbol, s_expressionsNamespaceName);
        }


        /// <summary>
        /// return true if the type is constructed from a generic interface that 
        /// might be implemented by an array.
        /// </summary>
        public static bool IsPossibleArrayGenericInterface(this TypeSymbol _type)
        {
            NamedTypeSymbol t = _type as NamedTypeSymbol;
            if ((object)t == null)
            {
                return false;
            }

            t = t.OriginalDefinition;

            SpecialType st = t.SpecialType;

            if (st == SpecialType.System_Collections_Generic_IList_T ||
                st == SpecialType.System_Collections_Generic_ICollection_T ||
                st == SpecialType.System_Collections_Generic_IEnumerable_T ||
                st == SpecialType.System_Collections_Generic_IReadOnlyList_T ||
                st == SpecialType.System_Collections_Generic_IReadOnlyCollection_T)
            {
                return true;
            }

            return false;
        }

        private static readonly string[] s_expressionsNamespaceName = { "Expressions", "Linq", MetadataHelpers.SystemString, "" };

        private static bool CheckFullName(Symbol symbol, string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if ((object)symbol == null || symbol.Name != names[i]) return false;
                symbol = symbol.ContainingSymbol;
            }

            return true;
        }

        public static bool IsDelegateType(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            return type.TypeKind == TypeKind.Delegate;
        }

        public static ImmutableArray<ParameterSymbol> DelegateParameters(this TypeSymbol type)
        {
            var invokeMethod = type.DelegateInvokeMethod();
            if ((object)invokeMethod == null)
            {
                return default(ImmutableArray<ParameterSymbol>);
            }
            return invokeMethod.Parameters;
        }

        public static bool TryGetElementTypesIfTupleOrCompatible(this TypeSymbol type, out ImmutableArray<TypeSymbolWithAnnotations> elementTypes)
        {
            if (type.IsTupleType)
            {
                elementTypes = ((TupleTypeSymbol)type).TupleElementTypes;
                return true;
            }

            // The following codepath should be very uncommon since it would be rare
            // to see a tuple underlying type not represented as a tuple.
            // It still might happen since tuple underlying types are creatable via public APIs 
            // and it is also possible that they would be passed in.

            // PERF: if allocations here become nuisance, consider caching the results
            //       in the type symbols that can actually be tuple compatible
            int cardinality;
            if (!type.IsTupleCompatible(out cardinality))
            {
                // source not a tuple or compatible
                elementTypes = default(ImmutableArray<TypeSymbolWithAnnotations>);
                return false;
            }

            var elementTypesBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(cardinality);
            TupleTypeSymbol.AddElementTypes((NamedTypeSymbol)type, elementTypesBuilder);

            Debug.Assert(elementTypesBuilder.Count == cardinality);

            elementTypes = elementTypesBuilder.ToImmutableAndFree();
            return true;
        }

        public static ImmutableArray<TypeSymbolWithAnnotations> GetElementTypesOfTupleOrCompatible(this TypeSymbol type)
        {
            if (type.IsTupleType)
            {
                return ((TupleTypeSymbol)type).TupleElementTypes;
            }

            // The following codepath should be very uncommon since it would be rare
            // to see a tuple underlying type not represented as a tuple.
            // It still might happen since tuple underlying types are creatable via public APIs 
            // and it is also possible that they would be passed in.

            Debug.Assert(type.IsTupleCompatible());

            // PERF: if allocations here become nuisance, consider caching the results
            //       in the type symbols that can actually be tuple compatible
            var elementTypesBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
            TupleTypeSymbol.AddElementTypes((NamedTypeSymbol)type, elementTypesBuilder);

            return elementTypesBuilder.ToImmutableAndFree();
        }

        public static MethodSymbol DelegateInvokeMethod(this TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.IsDelegateType() || type.IsExpressionTree());
            return type.GetDelegateType().DelegateInvokeMethod;
        }

        /// <summary>
        /// Return the default value constant for the given type,
        /// or null if the default value is not a constant.
        /// </summary>
        public static ConstantValue GetDefaultValue(this TypeSymbol type)
        {
            // SPEC:    A default-value-expression is a constant expression (§7.19) if the type
            // SPEC:    is a reference type or a type parameter that is known to be a reference type (§10.1.5). 
            // SPEC:    In addition, a default-value-expression is a constant expression if the type is
            // SPEC:    one of the following value types:
            // SPEC:    sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal, bool, or any enumeration type.

            Debug.Assert((object)type != null);

            if (type.IsErrorType())
            {
                return null;
            }

            if (type.IsReferenceType)
            {
                return ConstantValue.Null;
            }

            if (type.IsValueType)
            {
                if (type.IsEnumType())
                {
                    type = type.GetEnumUnderlyingType();
                }

                switch (type.SpecialType)
                {
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Char:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        return ConstantValue.Default(type.SpecialType);
                }
            }

            return null;
        }

        public static SpecialType GetSpecialTypeSafe(this TypeSymbol type)
        {
            return (object)type != null ? type.SpecialType : SpecialType.None;
        }

        public static bool IsAtLeastAsVisibleAs(this TypeSymbol type, Symbol sym, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            HashSet<DiagnosticInfo> localUseSiteDiagnostics = useSiteDiagnostics;
            var result = type.VisitType((type1, symbol, unused) => IsTypeLessVisibleThan(type1, symbol, ref localUseSiteDiagnostics), sym,
                                        canDigThroughNullable: true); // System.Nullable is public
            useSiteDiagnostics = localUseSiteDiagnostics;
            return (object)result == null;
        }

        private static bool IsTypeLessVisibleThan(TypeSymbol type, Symbol sym, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                case TypeKind.Submission:
                    return !IsAsRestrictive((NamedTypeSymbol)type, sym, ref useSiteDiagnostics);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Visit the given type and, in the case of compound types, visit all "sub type"
        /// (such as A in A[], or { A&lt;T&gt;, T, U } in A&lt;T&gt;.B&lt;U&gt;) invoking 'predicate'
        /// with the type and 'arg' at each sub type. If the predicate returns true for any type,
        /// traversal stops and that type is returned from this method. Otherwise if traversal
        /// completes without the predicate returning true for any type, this method returns null.
        /// </summary>
        public static TypeSymbol VisitType<T>(
            this TypeSymbol type,
            Func<TypeSymbol, T, bool, bool> predicate,
            T arg,
            bool canDigThroughNullable = false)
        {
            return VisitType(
                typeWithAnnotationsOpt: default,
                typeOpt: type,
                typeWithAnnotationsPredicateOpt: null,
                typePredicateOpt: predicate,
                arg,
                canDigThroughNullable);
        }

        /// <summary>
        /// Visit the given type and, in the case of compound types, visit all "sub type".
        /// One of the predicates will be invoked at each type. If the type is a
        /// TypeSymbolWithAnnotations, <paramref name="typeWithAnnotationsPredicateOpt"/>
        /// will be invoked; otherwise <paramref name="typePredicateOpt"/> will be invoked.
        /// If the corresponding predicate returns true for any type,
        /// traversal stops and that type is returned from this method. Otherwise if traversal
        /// completes without the predicate returning true for any type, this method returns null.
        /// </summary>
        public static TypeSymbol VisitType<T>(
            // https://github.com/dotnet/roslyn/issues/30059: If TypeSymbolWithAnnotations
            // is a struct, use a single type argument and a single predicate.
            this TypeSymbolWithAnnotations typeWithAnnotationsOpt,
            TypeSymbol typeOpt,
            Func<TypeSymbolWithAnnotations, T, bool, bool> typeWithAnnotationsPredicateOpt,
            Func<TypeSymbol, T, bool, bool> typePredicateOpt,
            T arg,
            bool canDigThroughNullable = false)
        {
            Debug.Assert(typeWithAnnotationsOpt.IsNull != (typeOpt is null));

            // In order to handle extremely "deep" types like "int[][][][][][][][][]...[]"
            // or int*****************...* we implement manual tail recursion rather than 
            // doing the natural recursion.

            while (true)
            {
                TypeSymbol current = typeOpt ?? typeWithAnnotationsOpt.TypeSymbol;
                bool isNestedNamedType = false;

                // Visit containing types from outer-most to inner-most.
                switch (current.TypeKind)
                {
                    case TypeKind.Class:
                    case TypeKind.Struct:
                    case TypeKind.Interface:
                    case TypeKind.Enum:
                    case TypeKind.Delegate:
                        {
                            var containingType = current.ContainingType;
                            if ((object)containingType != null)
                            {
                                isNestedNamedType = true;
                                var result = VisitType(default, containingType, typeWithAnnotationsPredicateOpt, typePredicateOpt, arg, canDigThroughNullable);
                                if ((object)result != null)
                                {
                                    return result;
                                }
                            }
                        }
                        break;

                    case TypeKind.Submission:
                        Debug.Assert((object)current.ContainingType == null);
                        break;
                }

                if (!typeWithAnnotationsOpt.IsNull && typeWithAnnotationsPredicateOpt != null)
                {
                    if (typeWithAnnotationsPredicateOpt(typeWithAnnotationsOpt, arg, isNestedNamedType))
                    {
                        return current;
                    }
                }
                else if (typePredicateOpt != null)
                {
                    if (typePredicateOpt(current, arg, isNestedNamedType))
                    {
                        return current;
                    }
                }

                TypeSymbolWithAnnotations next;

                switch (current.TypeKind)
                {
                    case TypeKind.Error:
                    case TypeKind.Dynamic:
                    case TypeKind.TypeParameter:
                    case TypeKind.Submission:
                    case TypeKind.Enum:
                        return null;

                    case TypeKind.Class:
                    case TypeKind.Struct:
                    case TypeKind.Interface:
                    case TypeKind.Delegate:
                        if (current.IsTupleType)
                        {
                            // turn tuple type elements into parameters
                            current = current.TupleUnderlyingType;
                        }

                        foreach (var typeArg in ((NamedTypeSymbol)current).TypeArgumentsNoUseSiteDiagnostics)
                        {
                            // Let's try to avoid early resolution of nullable types
                            var result = VisitType(
                                typeWithAnnotationsOpt: canDigThroughNullable ? default : typeArg,
                                typeOpt: canDigThroughNullable ? typeArg.NullableUnderlyingTypeOrSelf : null,
                                typeWithAnnotationsPredicateOpt,
                                typePredicateOpt,
                                arg,
                                canDigThroughNullable);
                            if ((object)result != null)
                            {
                                return result;
                            }
                        }
                        return null;

                    case TypeKind.Array:
                        next = ((ArrayTypeSymbol)current).ElementType;
                        break;

                    case TypeKind.Pointer:
                        next = ((PointerTypeSymbol)current).PointedAtType;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(current.TypeKind);
                }

                // Let's try to avoid early resolution of nullable types
                typeWithAnnotationsOpt = canDigThroughNullable ? default : next;
                typeOpt = canDigThroughNullable ? next.NullableUnderlyingTypeOrSelf : null;
            }
        }

        private static bool IsAsRestrictive(NamedTypeSymbol s1, Symbol sym2, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Accessibility acc1 = s1.DeclaredAccessibility;

            if (acc1 == Accessibility.Public)
            {
                return true;
            }

            for (Symbol s2 = sym2; s2.Kind != SymbolKind.Namespace; s2 = s2.ContainingSymbol)
            {
                Accessibility acc2 = s2.DeclaredAccessibility;

                switch (acc1)
                {
                    case Accessibility.Internal:
                        {
                            // If s2 is private or internal, and within the same assembly as s1,
                            // then this is at least as restrictive as s1's internal.
                            if ((acc2 == Accessibility.Private || acc2 == Accessibility.Internal || acc2 == Accessibility.ProtectedAndInternal) && s2.ContainingAssembly.HasInternalAccessTo(s1.ContainingAssembly))
                            {
                                return true;
                            }

                            break;
                        }

                    case Accessibility.ProtectedAndInternal:
                        // Since s1 is private protected, s2 must pass the test for being both more restrictive than internal and more restrictive than protected.
                        // We first do the "internal" test (copied from above), then if it passes we continue with the "protected" test.

                        if ((acc2 == Accessibility.Private || acc2 == Accessibility.Internal || acc2 == Accessibility.ProtectedAndInternal) && s2.ContainingAssembly.HasInternalAccessTo(s1.ContainingAssembly))
                        {
                            // passed the internal test; now do the test for the protected case
                            goto case Accessibility.Protected;
                        }

                        break;

                    case Accessibility.Protected:
                        {
                            var parent1 = s1.ContainingType;

                            if ((object)parent1 == null)
                            {
                                // not helpful
                            }
                            else if (acc2 == Accessibility.Private)
                            {
                                // if s2 is private and within s1's parent or within a subclass of s1's parent,
                                // then this is at least as restrictive as s1's protected.
                                for (var parent2 = s2.ContainingType; (object)parent2 != null; parent2 = parent2.ContainingType)
                                {
                                    if (parent1.IsAccessibleViaInheritance(parent2, ref useSiteDiagnostics))
                                    {
                                        return true;
                                    }
                                }
                            }
                            else if (acc2 == Accessibility.Protected || acc2 == Accessibility.ProtectedAndInternal)
                            {
                                // if s2 is protected, and it's parent is a subclass (or the same as) s1's parent
                                // then this is at least as restrictive as s1's protected
                                var parent2 = s2.ContainingType;
                                if ((object)parent2 != null && parent1.IsAccessibleViaInheritance(parent2, ref useSiteDiagnostics))
                                {
                                    return true;
                                }
                            }

                            break;
                        }

                    case Accessibility.ProtectedOrInternal:
                        {
                            var parent1 = s1.ContainingType;

                            if ((object)parent1 == null)
                            {
                                break;
                            }

                            switch (acc2)
                            {
                                case Accessibility.Private:
                                    // if s2 is private and within a subclass of s1's parent,
                                    // or within the same assembly as s1
                                    // then this is at least as restrictive as s1's internal protected.
                                    if (s2.ContainingAssembly.HasInternalAccessTo(s1.ContainingAssembly))
                                    {
                                        return true;
                                    }

                                    for (var parent2 = s2.ContainingType; (object)parent2 != null; parent2 = parent2.ContainingType)
                                    {
                                        if (parent1.IsAccessibleViaInheritance(parent2, ref useSiteDiagnostics))
                                        {
                                            return true;
                                        }
                                    }

                                    break;

                                case Accessibility.Internal:
                                    // If s2 is in the same assembly as s1, then this is more restrictive
                                    // than s1's internal protected.
                                    if (s2.ContainingAssembly.HasInternalAccessTo(s1.ContainingAssembly))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.Protected:
                                    // if s2 is protected, and it's parent is a subclass (or the same as) s1's parent
                                    // then this is at least as restrictive as s1's internal protected
                                    if (parent1.IsAccessibleViaInheritance(s2.ContainingType, ref useSiteDiagnostics))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.ProtectedAndInternal:
                                    // if s2 is private protected, and it's parent is a subclass (or the same as) s1's parent
                                    // or its in the same assembly as s1, then this is at least as restrictive as s1's protected
                                    if (s2.ContainingAssembly.HasInternalAccessTo(s1.ContainingAssembly) ||
                                        parent1.IsAccessibleViaInheritance(s2.ContainingType, ref useSiteDiagnostics))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.ProtectedOrInternal:
                                    // if s2 is internal protected, and it's parent is a subclass (or the same as) s1's parent
                                    // and its in the same assembly as s1, then this is at least as restrictive as s1's protected
                                    if (s2.ContainingAssembly.HasInternalAccessTo(s1.ContainingAssembly) &&
                                        parent1.IsAccessibleViaInheritance(s2.ContainingType, ref useSiteDiagnostics))
                                    {
                                        return true;
                                    }

                                    break;
                            }
                            break;
                        }

                    case Accessibility.Private:
                        if (acc2 == Accessibility.Private)
                        {
                            // if s2 is private, and it is within s1's parent, then this is at
                            // least as restrictive as s1's private.
                            NamedTypeSymbol parent1 = s1.ContainingType;

                            if ((object)parent1 == null)
                            {
                                break;
                            }

                            var parent1OriginalDefinition = parent1.OriginalDefinition;
                            for (var parent2 = s2.ContainingType; (object)parent2 != null; parent2 = parent2.ContainingType)
                            {
                                if (ReferenceEquals(parent2.OriginalDefinition, parent1OriginalDefinition) || parent1OriginalDefinition.TypeKind == TypeKind.Submission && parent2.TypeKind == TypeKind.Submission)
                                {
                                    return true;
                                }
                            }
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(acc1);
                }
            }

            return false;
        }

        public static bool IsUnboundGenericType(this TypeSymbol type)
        {
            var namedType = type as NamedTypeSymbol;
            return (object)namedType != null && namedType.IsUnboundGenericType;
        }

        public static bool IsTopLevelType(this NamedTypeSymbol type)
        {
            return (object)type.ContainingType == null;
        }

        /// <summary>
        /// (null TypeParameterSymbol "parameter"): Checks if the given type is a type parameter 
        /// or its referent type is a type parameter (array/pointer) or contains a type parameter (aggregate type)
        /// (non-null TypeParameterSymbol "parameter"): above + also checks if the type parameter
        /// is the same as "parameter"
        /// </summary>
        public static bool ContainsTypeParameter(this TypeSymbol type, TypeParameterSymbol parameter = null)
        {
            var result = type.VisitType(s_containsTypeParameterPredicate, parameter);
            return (object)result != null;
        }

        private static readonly Func<TypeSymbol, TypeParameterSymbol, bool, bool> s_containsTypeParameterPredicate =
            (type, parameter, unused) => type.TypeKind == TypeKind.TypeParameter && ((object)parameter == null || TypeSymbol.Equals(type, parameter, TypeCompareKind.ConsiderEverything2));

        public static bool ContainsTypeParameter(this TypeSymbol type, MethodSymbol parameterContainer)
        {
            Debug.Assert((object)parameterContainer != null);

            var result = type.VisitType(s_isTypeParameterWithSpecificContainerPredicate, parameterContainer);
            return (object)result != null;
        }

        private static readonly Func<TypeSymbol, Symbol, bool, bool> s_isTypeParameterWithSpecificContainerPredicate =
             (type, parameterContainer, unused) => type.TypeKind == TypeKind.TypeParameter && (object)type.ContainingSymbol == (object)parameterContainer;

        public static bool ContainsTypeParameters(this TypeSymbol type, HashSet<TypeParameterSymbol> parameters)
        {
            var result = type.VisitType(s_containsTypeParametersPredicate, parameters);
            return (object)result != null;
        }

        private static readonly Func<TypeSymbol, HashSet<TypeParameterSymbol>, bool, bool> s_containsTypeParametersPredicate =
            (type, parameters, unused) => type.TypeKind == TypeKind.TypeParameter && parameters.Contains((TypeParameterSymbol)type);

        /// <summary>
        /// Return true if the type contains any dynamic type reference.
        /// </summary>
        public static bool ContainsDynamic(this TypeSymbol type)
        {
            var result = type.VisitType(s_containsDynamicPredicate, null, canDigThroughNullable: true);
            return (object)result != null;
        }

        private static readonly Func<TypeSymbol, object, bool, bool> s_containsDynamicPredicate = (type, unused1, unused2) => type.TypeKind == TypeKind.Dynamic;

        /// <summary>
        /// Return true if the type contains any tuples.
        /// </summary>
        internal static bool ContainsTuple(this TypeSymbol type) =>
            (object)type.VisitType((TypeSymbol t, object _1, bool _2) => t.IsTupleType, null) != null;

        /// <summary>
        /// Return true if the type contains any tuples with element names.
        /// </summary>
        internal static bool ContainsTupleNames(this TypeSymbol type) =>
            (object)type.VisitType((TypeSymbol t, object _1, bool _2) => !t.TupleElementNames.IsDefault, null) != null;

        /// <summary>
        /// Guess the non-error type that the given type was intended to represent.
        /// If the type itself is not an error type, then it will be returned.
        /// Otherwise, the underlying type (if any) of the error type will be
        /// returned.
        /// </summary>
        /// <remarks>
        /// Any non-null type symbol returned is guaranteed not to be an error type.
        /// 
        /// It is possible to pass in a constructed type and received back an 
        /// unconstructed type.  This can occur when the type passed in was
        /// constructed from an error type - the underlying definition will be
        /// available, but there won't be a good way to "re-substitute" back up
        /// to the level of the specified type.
        /// </remarks>
        internal static TypeSymbol GetNonErrorGuess(this TypeSymbol type)
        {
            var result = ExtendedErrorTypeSymbol.ExtractNonErrorType(type);
            Debug.Assert((object)result == null || !result.IsErrorType());
            return result;
        }

        /// <summary>
        /// Guess the non-error type kind that the given type was intended to represent,
        /// if possible. If not, return TypeKind.Error.
        /// </summary>
        internal static TypeKind GetNonErrorTypeKindGuess(this TypeSymbol type)
        {
            return ExtendedErrorTypeSymbol.ExtractNonErrorTypeKind(type);
        }

        /// <summary>
        /// Returns true if the type was a valid switch expression type in C# 6. We use this test to determine
        /// whether or not we should attempt a user-defined conversion from the type to a C# 6 switch governing
        /// type, which we support for compatibility with C# 6 and earlier.
        /// </summary>
        internal static bool IsValidV6SwitchGoverningType(this TypeSymbol type, bool isTargetTypeOfUserDefinedOp = false)
        {
            // SPEC:    The governing type of a switch statement is established by the switch expression.
            // SPEC:    1) If the type of the switch expression is sbyte, byte, short, ushort, int, uint,
            // SPEC:       long, ulong, bool, char, string, or an enum-type, or if it is the nullable type
            // SPEC:       corresponding to one of these types, then that is the governing type of the switch statement. 
            // SPEC:    2) Otherwise, exactly one user-defined implicit conversion must exist from the
            // SPEC:       type of the switch expression to one of the following possible governing types:
            // SPEC:       sbyte, byte, short, ushort, int, uint, long, ulong, char, string, or, a nullable type
            // SPEC:       corresponding to one of those types

            Debug.Assert((object)type != null);
            if (type.IsNullableType())
            {
                type = type.GetNullableUnderlyingType();
            }

            // User-defined implicit conversion with target type as Enum type is not valid.
            if (!isTargetTypeOfUserDefinedOp && type.IsEnumType())
            {
                type = type.GetEnumUnderlyingType();
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                    return true;

                case SpecialType.System_Boolean:
                    // User-defined implicit conversion with target type as bool type is not valid.
                    return !isTargetTypeOfUserDefinedOp;
            }

            return false;
        }

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
        /// <summary>
        /// Returns true if the type is one of the restricted types, namely: <see cref="T:System.TypedReference"/>, 
        /// <see cref="T:System.ArgIterator"/>, or <see cref="T:System.RuntimeArgumentHandle"/>.
        /// or a ref-like type.
        /// </summary>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
        internal static bool IsRestrictedType(this TypeSymbol type,
                                                bool ignoreSpanLikeTypes = false)
        {
            // See Dev10 C# compiler, "type.cpp", bool Type::isSpecialByRefType() const
            Debug.Assert((object)type != null);
            switch (type.SpecialType)
            {
                case SpecialType.System_TypedReference:
                case SpecialType.System_ArgIterator:
                case SpecialType.System_RuntimeArgumentHandle:
                    return true;
            }

            return ignoreSpanLikeTypes ?
                        false :
                        type.IsRefLikeType;
        }

        public static bool IsIntrinsicType(this TypeSymbol type)
        {
            return type.SpecialType.IsIntrinsicType();
        }

        public static bool IsPartial(this TypeSymbol type)
        {
            var nt = type as SourceNamedTypeSymbol;
            return (object)nt != null && nt.IsPartial;
        }

        public static bool IsPointerType(this TypeSymbol type)
        {
            return type is PointerTypeSymbol;
        }

        internal static int FixedBufferElementSizeInBytes(this TypeSymbol type)
        {
            return type.SpecialType.FixedBufferElementSizeInBytes();
        }

        // check that its type is allowed for Volatile
        internal static bool IsValidVolatileFieldType(this TypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Struct:
                    return type.SpecialType.IsValidVolatileFieldType();

                case TypeKind.Array:
                case TypeKind.Class:
                case TypeKind.Delegate:
                case TypeKind.Dynamic:
                case TypeKind.Error:
                case TypeKind.Interface:
                case TypeKind.Pointer:
                    return true;

                case TypeKind.Enum:
                    return ((NamedTypeSymbol)type).EnumUnderlyingType.SpecialType.IsValidVolatileFieldType();

                case TypeKind.TypeParameter:
                    return type.IsReferenceType;

                case TypeKind.Submission:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }

            return false;
        }

        /// <summary>
        /// Add this instance to the set of checked types. Returns true
        /// if this was added, false if the type was already in the set.
        /// </summary>
        public static bool MarkCheckedIfNecessary(this TypeSymbol type, ref HashSet<TypeSymbol> checkedTypes)
        {
            if (checkedTypes == null)
            {
                checkedTypes = new HashSet<TypeSymbol>();
            }

            return checkedTypes.Add(type);
        }

        internal static bool IsUnsafe(this TypeSymbol type)
        {
            while (true)
            {
                switch (type.TypeKind)
                {
                    case TypeKind.Pointer:
                        return true;
                    case TypeKind.Array:
                        type = ((ArrayTypeSymbol)type).ElementType.TypeSymbol;
                        break;
                    default:
                        // NOTE: we could consider a generic type with unsafe type arguments to be unsafe,
                        // but that's already an error, so there's no reason to report it.  Also, this
                        // matches Type::isUnsafe in Dev10.
                        return false;
                }
            }
        }

        internal static bool IsVoidPointer(this TypeSymbol type)
        {
            return type.IsPointerType() && ((PointerTypeSymbol)type).PointedAtType.SpecialType == SpecialType.System_Void;
        }

        /// <summary>
        /// These special types are structs that contain fields of the same type
        /// (e.g. <see cref="System.Int32"/> contains an instance field of type <see cref="System.Int32"/>).
        /// </summary>
        internal static bool IsPrimitiveRecursiveStruct(this TypeSymbol t)
        {
            return t.SpecialType.IsPrimitiveRecursiveStruct();
        }

        /// <summary>
        /// Compute a hash code for the constructed type. The return value will be
        /// non-zero so callers can used zero to represent an uninitialized value.
        /// </summary>
        internal static int ComputeHashCode(this NamedTypeSymbol type)
        {
            int code = type.OriginalDefinition.GetHashCode();
            code = Hash.Combine(type.ContainingType, code);

            // Unconstructed type may contain alpha-renamed type parameters while
            // may still be considered equal, we do not want to give different hashcode to such types.
            //
            // Example:
            //   Having original type A<U>.B<V> we create two _unconstructed_ types
            //    A<int>.B<V'>
            //    A<int>.B<V">     
            //  Note that V' and V" are type parameters substituted via alpha-renaming of original V
            //  These are different objects, but represent the same "type parameter at index 1"
            //
            //  In short - we are not interested in the type parameters of unconstructed types.
            if ((object)type.ConstructedFrom != (object)type)
            {
                foreach (var arg in type.TypeArgumentsNoUseSiteDiagnostics)
                {
                    code = Hash.Combine(arg.TypeSymbol, code);
                }
            }

            // 0 may be used by the caller to indicate the hashcode is not
            // initialized. If we computed 0 for the hashcode, tweak it.
            if (code == 0)
            {
                code++;
            }
            return code;
        }

        /// <summary>
        /// If we are in a COM PIA with embedInteropTypes enabled we should turn properties and methods 
        /// that have the type and return type of object, respectively, into type dynamic. If the requisite conditions 
        /// are fulfilled, this method returns a dynamic type. If not, it returns the original type.
        /// </summary>
        /// <param name="type">A property type or method return type to be checked for dynamification.</param>
        /// <param name="containingType">Containing type.</param>
        /// <returns></returns>
        public static TypeSymbol AsDynamicIfNoPia(this TypeSymbol type, NamedTypeSymbol containingType)
        {
            return type.TryAsDynamicIfNoPia(containingType, out TypeSymbol result) ? result : type;
        }

        public static bool TryAsDynamicIfNoPia(this TypeSymbol type, NamedTypeSymbol containingType, out TypeSymbol result)
        {
            if (type.SpecialType == SpecialType.System_Object)
            {
                AssemblySymbol assembly = containingType.ContainingAssembly;
                if ((object)assembly != null &&
                    assembly.IsLinked &&
                    containingType.IsComImport)
                {
                    result = DynamicTypeSymbol.Instance;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Type variables are never considered reference types by the verifier.
        /// </summary>
        internal static bool IsVerifierReference(this TypeSymbol type)
        {
            return type.IsReferenceType && type.TypeKind != TypeKind.TypeParameter;
        }

        /// <summary>
        /// Type variables are never considered value types by the verifier.
        /// </summary>
        internal static bool IsVerifierValue(this TypeSymbol type)
        {
            return type.IsValueType && type.TypeKind != TypeKind.TypeParameter;
        }

        internal static void AddUseSiteDiagnostics(
            this TypeSymbol type,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            DiagnosticInfo errorInfo = type.GetUseSiteDiagnostic();

            if ((object)errorInfo != null)
            {
                if (useSiteDiagnostics == null)
                {
                    useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                }

                useSiteDiagnostics.Add(errorInfo);
            }
        }

        /// <summary>
        /// Return all of the type parameters in this type and enclosing types,
        /// from outer-most to inner-most type.
        /// </summary>
        internal static ImmutableArray<TypeParameterSymbol> GetAllTypeParameters(this NamedTypeSymbol type)
        {
            // Avoid allocating a builder in the common case.
            if ((object)type.ContainingType == null)
            {
                return type.TypeParameters;
            }

            var builder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            type.GetAllTypeParameters(builder);
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Return all of the type parameters in this type and enclosing types,
        /// from outer-most to inner-most type.
        /// </summary>
        internal static void GetAllTypeParameters(this NamedTypeSymbol type, ArrayBuilder<TypeParameterSymbol> result)
        {
            var containingType = type.ContainingType;
            if ((object)containingType != null)
            {
                containingType.GetAllTypeParameters(result);
            }

            result.AddRange(type.TypeParameters);
        }

        /// <summary>
        /// Return the nearest type parameter with the given name in
        /// this type or any enclosing type.
        /// </summary>
        internal static TypeParameterSymbol FindEnclosingTypeParameter(this NamedTypeSymbol type, string name)
        {
            var allTypeParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            type.GetAllTypeParameters(allTypeParameters);

            TypeParameterSymbol result = null;

            foreach (TypeParameterSymbol tpEnclosing in allTypeParameters)
            {
                if (name == tpEnclosing.Name)
                {
                    result = tpEnclosing;
                    break;
                }
            }

            allTypeParameters.Free();
            return result;
        }

        /// <summary>
        /// Return the nearest type parameter with the given name in
        /// this symbol or any enclosing symbol.
        /// </summary>
        internal static TypeParameterSymbol FindEnclosingTypeParameter(this Symbol methodOrType, string name)
        {
            while (methodOrType != null)
            {
                switch (methodOrType.Kind)
                {
                    case SymbolKind.Method:
                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                    case SymbolKind.Field:
                    case SymbolKind.Property:
                    case SymbolKind.Event:
                        break;
                    default:
                        return null;
                }
                foreach (var typeParameter in methodOrType.GetMemberTypeParameters())
                {
                    if (typeParameter.Name == name)
                    {
                        return typeParameter;
                    }
                }
                methodOrType = methodOrType.ContainingSymbol;
            }
            return null;
        }

        /// <summary>
        /// Return true if the fully qualified name of the type's containing symbol
        /// matches the given name. This method avoids string concatenations
        /// in the common case where the type is a top-level type.
        /// </summary>
        internal static bool HasNameQualifier(this NamedTypeSymbol type, string qualifiedName)
        {
            const StringComparison comparison = StringComparison.Ordinal;

            var container = type.ContainingSymbol;
            if (container.Kind != SymbolKind.Namespace)
            {
                // Nested type. For simplicity, compare qualified name to SymbolDisplay result.
                return string.Equals(container.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat), qualifiedName, comparison);
            }

            var @namespace = (NamespaceSymbol)container;
            if (@namespace.IsGlobalNamespace)
            {
                return qualifiedName.Length == 0;
            }

            return HasNamespaceName(@namespace, qualifiedName, comparison, length: qualifiedName.Length);
        }

        private static bool HasNamespaceName(NamespaceSymbol @namespace, string namespaceName, StringComparison comparison, int length)
        {
            if (length == 0)
            {
                return false;
            }

            var container = @namespace.ContainingNamespace;
            int separator = namespaceName.LastIndexOf('.', length - 1, length);
            int offset = 0;
            if (separator >= 0)
            {
                if (container.IsGlobalNamespace)
                {
                    return false;
                }

                if (!HasNamespaceName(container, namespaceName, comparison, length: separator))
                {
                    return false;
                }

                int n = separator + 1;
                offset = n;
                length -= n;
            }
            else if (!container.IsGlobalNamespace)
            {
                return false;
            }

            var name = @namespace.Name;
            return (name.Length == length) && (string.Compare(name, 0, namespaceName, offset, length, comparison) == 0);
        }

        internal static bool IsNonGenericTaskType(this TypeSymbol type, CSharpCompilation compilation)
        {
            var namedType = type as NamedTypeSymbol;
            if ((object)namedType == null || namedType.Arity != 0)
            {
                return false;
            }
            if ((object)namedType == compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task))
            {
                return true;
            }
            if (namedType.SpecialType == SpecialType.System_Void)
            {
                return false;
            }
            object builderArgument;
            return namedType.IsCustomTaskType(out builderArgument);
        }

        internal static bool IsGenericTaskType(this TypeSymbol type, CSharpCompilation compilation)
        {
            var namedType = type as NamedTypeSymbol;
            if ((object)namedType == null || namedType.Arity != 1)
            {
                return false;
            }
            if ((object)namedType.ConstructedFrom == compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T))
            {
                return true;
            }
            object builderArgument;
            return namedType.IsCustomTaskType(out builderArgument);
        }

        internal static bool IsIAsyncEnumerableType(this TypeSymbol type, CSharpCompilation compilation)
        {
            var namedType = type as NamedTypeSymbol;
            if ((object)namedType == null || namedType.Arity != 1)
            {
                return false;
            }

            return (object)namedType.ConstructedFrom == compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T);
        }

        internal static bool IsIAsyncEnumeratorType(this TypeSymbol type, CSharpCompilation compilation)
        {
            var namedType = type as NamedTypeSymbol;
            if ((object)namedType == null || namedType.Arity != 1)
            {
                return false;
            }

            return (object)namedType.ConstructedFrom == compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T);
        }

        /// <summary>
        /// Returns true if the type is generic or non-generic custom task-like type due to the
        /// [AsyncMethodBuilder(typeof(B))] attribute. It returns the "B".
        /// </summary>
        /// <remarks>
        /// For the Task types themselves, this method might return true or false depending on mscorlib.
        /// The definition of "custom task-like type" is one that has an [AsyncMethodBuilder(typeof(B))] attribute,
        /// no more, no less. Validation of builder type B is left for elsewhere. This method returns B
        /// without validation of any kind.
        /// </remarks>
        internal static bool IsCustomTaskType(this NamedTypeSymbol type, out object builderArgument)
        {
            Debug.Assert((object)type != null);

            var arity = type.Arity;
            if (arity < 2)
            {
                // Find the AsyncBuilder attribute.
                foreach (var attr in type.GetAttributes())
                {
                    if (attr.IsTargetAttribute(type, AttributeDescription.AsyncMethodBuilderAttribute)
                        && attr.CommonConstructorArguments.Length == 1
                        && attr.CommonConstructorArguments[0].Kind == TypedConstantKind.Type)
                    {
                        builderArgument = attr.CommonConstructorArguments[0].Value;
                        return true;
                    }
                }
            }

            builderArgument = null;
            return false;
        }

        /// <summary>
        /// Replace Task-like types with Task types.
        /// </summary>
        internal static TypeSymbol NormalizeTaskTypes(this TypeSymbol type, CSharpCompilation compilation)
        {
            NormalizeTaskTypesInType(compilation, ref type);
            return type;
        }

        /// <summary>
        /// Replace Task-like types with Task types. Returns true if there were changes.
        /// </summary>
        private static bool NormalizeTaskTypesInType(CSharpCompilation compilation, ref TypeSymbol type)
        {
            switch (type.Kind)
            {
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    {
                        var namedType = (NamedTypeSymbol)type;
                        var changed = type.IsTupleType ?
                            NormalizeTaskTypesInTuple(compilation, ref namedType) :
                            NormalizeTaskTypesInNamedType(compilation, ref namedType);
                        type = namedType;
                        return changed;
                    }
                case SymbolKind.ArrayType:
                    {
                        var arrayType = (ArrayTypeSymbol)type;
                        var changed = NormalizeTaskTypesInArray(compilation, ref arrayType);
                        type = arrayType;
                        return changed;
                    }
                case SymbolKind.PointerType:
                    {
                        var pointerType = (PointerTypeSymbol)type;
                        var changed = NormalizeTaskTypesInPointer(compilation, ref pointerType);
                        type = pointerType;
                        return changed;
                    }
            }
            return false;
        }

        private static bool NormalizeTaskTypesInType(CSharpCompilation compilation, ref TypeSymbolWithAnnotations typeWithAnnotations)
        {
            var type = typeWithAnnotations.TypeSymbol;
            if (NormalizeTaskTypesInType(compilation, ref type))
            {
                typeWithAnnotations = TypeSymbolWithAnnotations.Create(type, customModifiers: typeWithAnnotations.CustomModifiers);
                return true;
            }
            return false;
        }

        private static bool NormalizeTaskTypesInNamedType(CSharpCompilation compilation, ref NamedTypeSymbol type)
        {
            bool hasChanged = false;

            if (!type.IsDefinition)
            {
                Debug.Assert(type.IsGenericType);
                var typeArgumentsBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                type.GetAllTypeArguments(typeArgumentsBuilder, ref useSiteDiagnostics);
                for (int i = 0; i < typeArgumentsBuilder.Count; i++)
                {
                    var typeWithModifier = typeArgumentsBuilder[i];
                    var typeArgNormalized = typeWithModifier.TypeSymbol;
                    if (NormalizeTaskTypesInType(compilation, ref typeArgNormalized))
                    {
                        hasChanged = true;
                        // Preserve custom modifiers but without normalizing those types.
                        typeArgumentsBuilder[i] = TypeSymbolWithAnnotations.Create(typeArgNormalized, customModifiers: typeWithModifier.CustomModifiers);
                    }
                }
                if (hasChanged)
                {
                    var originalDefinition = type.OriginalDefinition;
                    var typeParameters = originalDefinition.GetAllTypeParameters();
                    var typeMap = new TypeMap(typeParameters, typeArgumentsBuilder.ToImmutable(), allowAlpha: true);
                    type = typeMap.SubstituteNamedType(originalDefinition);
                }
                typeArgumentsBuilder.Free();
            }

            object builderArgument;
            if (type.OriginalDefinition.IsCustomTaskType(out builderArgument))
            {
                int arity = type.Arity;
                Debug.Assert(arity < 2);
                var taskType = compilation.GetWellKnownType(
                    arity == 0 ?
                    WellKnownType.System_Threading_Tasks_Task :
                    WellKnownType.System_Threading_Tasks_Task_T);
                if (taskType.TypeKind == TypeKind.Error)
                {
                    // Skip if Task types are not available.
                    return false;
                }
                type = arity == 0 ?
                    taskType :
                    taskType.Construct(
                        ImmutableArray.Create(type.TypeArgumentsNoUseSiteDiagnostics[0]),
                        unbound: false);
                hasChanged = true;
            }

            return hasChanged;
        }

        private static bool NormalizeTaskTypesInTuple(CSharpCompilation compilation, ref NamedTypeSymbol type)
        {
            Debug.Assert(type.IsTupleType);
            var underlyingType = type.TupleUnderlyingType;
            if (!NormalizeTaskTypesInNamedType(compilation, ref underlyingType))
            {
                return false;
            }
            type = TupleTypeSymbol.Create(underlyingType, type.TupleElementNames);
            return true;
        }

        private static bool NormalizeTaskTypesInArray(CSharpCompilation compilation, ref ArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;
            if (!NormalizeTaskTypesInType(compilation, ref elementType))
            {
                return false;
            }
            arrayType = arrayType.WithElementType(elementType);
            return true;
        }

        private static bool NormalizeTaskTypesInPointer(CSharpCompilation compilation, ref PointerTypeSymbol pointerType)
        {
            var pointedAtType = pointerType.PointedAtType;
            if (!NormalizeTaskTypesInType(compilation, ref pointedAtType))
            {
                return false;
            }
            // Preserve custom modifiers but without normalizing those types.
            pointerType = new PointerTypeSymbol(pointedAtType);
            return true;
        }

        internal static Cci.TypeReferenceWithAttributes GetTypeRefWithAttributes(
            this TypeSymbolWithAnnotations type,
            Emit.PEModuleBuilder moduleBuilder,
            Symbol declaringSymbol,
            Cci.ITypeReference typeRef)
        {
            var builder = ArrayBuilder<Cci.ICustomAttribute>.GetInstance();
            if (type.TypeSymbol.ContainsTupleNames())
            {
                SynthesizedAttributeData attr = declaringSymbol.DeclaringCompilation.SynthesizeTupleNamesAttribute(type.TypeSymbol);
                if (attr != null)
                {
                    builder.Add(attr);
                }
            }

            if (type.NeedsNullableAttribute())
            {
                SynthesizedAttributeData attr = moduleBuilder.SynthesizeNullableAttribute(declaringSymbol, type);
                if (attr != null)
                {
                    builder.Add(attr);
                }
            }
            return new Cci.TypeReferenceWithAttributes(typeRef, builder.ToImmutableAndFree());
        }

        internal static bool IsWellKnownTypeInAttribute(this ITypeSymbol typeSymbol) => typeSymbol.IsWellKnownInteropServicesTopLevelType("InAttribute");

        internal static bool IsWellKnownTypeUnmanagedType(this ITypeSymbol typeSymbol) => typeSymbol.IsWellKnownInteropServicesTopLevelType("UnmanagedType");

        private static bool IsWellKnownInteropServicesTopLevelType(this ITypeSymbol typeSymbol, string name)
        {
            if (typeSymbol.Name != name || typeSymbol.ContainingType != null)
            {
                return false;
            }

            var interopServicesNamespace = typeSymbol.ContainingNamespace;
            if (interopServicesNamespace?.Name != "InteropServices")
            {
                return false;
            }

            var runtimeNamespace = interopServicesNamespace.ContainingNamespace;
            if (runtimeNamespace?.Name != "Runtime")
            {
                return false;
            }

            var systemNamespace = runtimeNamespace.ContainingNamespace;
            if (systemNamespace?.Name != "System")
            {
                return false;
            }

            var globalNamespace = systemNamespace.ContainingNamespace;

            return globalNamespace != null && globalNamespace.IsGlobalNamespace;
        }

        public static bool IsBadAsyncReturn(this TypeSymbol returnType, CSharpCompilation declaringCompilation)
        {
            // Note: we're passing the return type explicitly (rather than using `method.ReturnType`) to avoid cycles
            return !returnType.IsErrorType() &&
                returnType.SpecialType != SpecialType.System_Void &&
                !returnType.IsNonGenericTaskType(declaringCompilation) &&
                !returnType.IsGenericTaskType(declaringCompilation) &&
                !returnType.IsIAsyncEnumerableType(declaringCompilation) &&
                !returnType.IsIAsyncEnumeratorType(declaringCompilation);
        }
    }
}
