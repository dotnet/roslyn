// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class BaseTypeAnalysis
    {
        internal static bool TypeDependsOn(NamedTypeSymbol depends, NamedTypeSymbol on)
        {
            Debug.Assert((object)depends != null);
            Debug.Assert((object)on != null);
            Debug.Assert(on.IsDefinition);

            var hs = PooledHashSet<Symbol>.GetInstance();
            TypeDependsClosure(depends, depends.DeclaringCompilation, hs);

            var result = hs.Contains(on);
            hs.Free();

            return result;
        }

        private static void TypeDependsClosure(NamedTypeSymbol type, CSharpCompilation currentCompilation, HashSet<Symbol> partialClosure)
        {
            if ((object)type == null)
            {
                return;
            }

            type = type.OriginalDefinition;
            if (partialClosure.Add(type))
            {
                if (type.IsInterface)
                {
                    foreach (var bt in type.GetDeclaredInterfaces(null))
                    {
                        TypeDependsClosure(bt, currentCompilation, partialClosure);
                    }
                }
                else
                {
                    TypeDependsClosure(type.GetDeclaredBaseType(null), currentCompilation, partialClosure);
                }

                // containment is interesting only for the current compilation
                if (currentCompilation != null && type.IsFromCompilation(currentCompilation))
                {
                    TypeDependsClosure(type.ContainingType, currentCompilation, partialClosure);
                }
            }
        }

        internal static bool StructDependsOn(NamedTypeSymbol depends, NamedTypeSymbol on)
        {
            Debug.Assert((object)depends != null);
            Debug.Assert((object)on != null);
            Debug.Assert(on.IsDefinition);

            var hs = PooledHashSet<Symbol>.GetInstance();
            StructDependsClosure(depends, hs, on);

            var result = hs.Contains(on);
            hs.Free();

            return result;
        }

        private static void StructDependsClosure(NamedTypeSymbol type, HashSet<Symbol> partialClosure, NamedTypeSymbol on)
        {
            Debug.Assert((object)type != null);

            if ((object)type.OriginalDefinition == on)
            {
                // found a possibly expanding cycle, for example
                //     struct X<T> { public T t; }
                //     struct W<T> { X<W<W<T>>> x; }
                // while not explicitly forbidden by the spec, it should be.
                partialClosure.Add(on);
                return;
            }

            if (partialClosure.Add(type))
            {
                foreach (var member in type.GetMembersUnordered())
                {
                    var field = member as FieldSymbol;
                    var fieldType = field?.NonPointerType();
                    if (fieldType is null || fieldType.TypeKind != TypeKind.Struct || field.IsStatic)
                    {
                        continue;
                    }

                    StructDependsClosure((NamedTypeSymbol)fieldType, partialClosure, on);
                }
            }
        }

        /// <summary>
        /// IsManagedType is simple for most named types:
        ///     enums are not managed;
        ///     non-enum, non-struct named types are managed;
        ///     type parameters are managed unless an 'unmanaged' constraint is present;
        ///     all special types have spec'd values (basically, (non-string) primitives) are not managed;
        /// 
        /// Only structs are complicated, because the definition is recursive.  A struct type is managed
        /// if one of its instance fields is managed.  Unfortunately, this can result in infinite recursion.
        /// If the closure is finite, and we don't find anything definitely managed, then we return true.
        /// If the closure is infinite, we disregard all but a representative of any expanding cycle.
        /// 
        /// Intuitively, this will only return true if there's a specific type we can point to that is would
        /// be managed even if it had no fields.  e.g. struct S { S s; } is not managed, but struct S { S s; object o; }
        /// is because we can point to object.
        /// </summary>
        internal static ManagedKind GetManagedKind(NamedTypeSymbol type)
        {
            var (isManaged, hasGenerics) = IsManagedTypeHelper(type);
            var definitelyManaged = isManaged == ThreeState.True;
            if (isManaged == ThreeState.Unknown)
            {
                // Otherwise, we have to build and inspect the closure of depended-upon types.
                var hs = PooledHashSet<Symbol>.GetInstance();
                var result = DependsOnDefinitelyManagedType(type, hs);
                definitelyManaged = result.definitelyManaged;
                hasGenerics = hasGenerics || result.hasGenerics;
                hs.Free();
            }


            if (definitelyManaged)
            {
                return ManagedKind.Managed;
            }
            else if (hasGenerics)
            {
                return ManagedKind.UnmanagedWithGenerics;
            }
            else
            {
                return ManagedKind.Unmanaged;
            }
        }

        // NOTE: If we do not check HasPointerType, we will unconditionally
        //       bind Type and that may cause infinite recursion.
        //       HasPointerType can use syntax directly and break recursion.
        internal static TypeSymbol NonPointerType(this FieldSymbol field) =>
            field.HasPointerType ? null : field.Type;

        private static (bool definitelyManaged, bool hasGenerics) DependsOnDefinitelyManagedType(NamedTypeSymbol type, HashSet<Symbol> partialClosure)
        {
            Debug.Assert((object)type != null);

            var hasGenerics = false;
            if (partialClosure.Add(type))
            {
                foreach (var member in type.GetInstanceFieldsAndEvents())
                {
                    // Only instance fields (including field-like events) affect the outcome.
                    FieldSymbol field;
                    switch (member.Kind)
                    {
                        case SymbolKind.Field:
                            field = (FieldSymbol)member;
                            Debug.Assert((object)(field.AssociatedSymbol as EventSymbol) == null,
                                "Didn't expect to find a field-like event backing field in the member list.");
                            break;
                        case SymbolKind.Event:
                            field = ((EventSymbol)member).AssociatedField;
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(member.Kind);
                    }

                    if ((object)field == null)
                    {
                        continue;
                    }

                    TypeSymbol fieldType = field.NonPointerType();
                    if (fieldType is null)
                    {
                        // pointers are unmanaged
                        continue;
                    }

                    NamedTypeSymbol fieldNamedType = fieldType as NamedTypeSymbol;
                    if ((object)fieldNamedType == null)
                    {
                        if (fieldType.IsManagedType)
                        {
                            return (true, hasGenerics);
                        }
                    }
                    else
                    {
                        var result = IsManagedTypeHelper(fieldNamedType);
                        hasGenerics = hasGenerics || result.hasGenerics;
                        // NOTE: don't use ManagedKind.get on a NamedTypeSymbol - that could lead
                        // to infinite recursion.
                        switch (result.isManaged)
                        {
                            case ThreeState.True:
                                return (true, hasGenerics);

                            case ThreeState.False:
                                continue;

                            case ThreeState.Unknown:
                                if (!fieldNamedType.OriginalDefinition.KnownCircularStruct)
                                {
                                    var (definitelyManaged, childHasGenerics) = DependsOnDefinitelyManagedType(fieldNamedType, partialClosure);
                                    hasGenerics = hasGenerics || childHasGenerics;
                                    if (definitelyManaged)
                                    {
                                        return (true, hasGenerics);
                                    }
                                }
                                continue;
                        }
                    }
                }
            }

            return (false, hasGenerics);
        }

        /// <summary>
        /// Returns True or False if we can determine whether the type is managed
        /// without looking at its fields and Unknown otherwise.
        /// Also returns whether or not the given type is generic.
        /// </summary>
        private static (ThreeState isManaged, bool hasGenerics) IsManagedTypeHelper(NamedTypeSymbol type)
        {
            // To match dev10, we treat enums as their underlying types.
            if (type.IsEnumType())
            {
                type = type.GetEnumUnderlyingType();
            }

            // Short-circuit common cases.
            switch (type.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_TypedReference:
                case SpecialType.System_ArgIterator:
                case SpecialType.System_RuntimeArgumentHandle:
                    return (ThreeState.False, false);
                case SpecialType.None:
                default:
                    // CONSIDER: could provide cases for other common special types.
                    break; // Proceed with additional checks.
            }

            bool hasGenerics = type.TupleUnderlyingTypeOrSelf() is NamedTypeSymbol { IsGenericType: true };
            switch (type.TypeKind)
            {
                case TypeKind.Enum:
                    return (ThreeState.False, hasGenerics);
                case TypeKind.Struct:
                    return (ThreeState.Unknown, hasGenerics);
                default:
                    return (ThreeState.True, hasGenerics);
            }
        }
    }
}
