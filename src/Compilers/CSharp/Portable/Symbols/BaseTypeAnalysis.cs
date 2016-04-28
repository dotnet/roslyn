// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class BaseTypeAnalysis
    {
        internal static bool ClassDependsOn(NamedTypeSymbol depends, NamedTypeSymbol on)
        {
            Debug.Assert((object)depends != null);
            Debug.Assert((object)on != null);
            Debug.Assert(on.IsDefinition);

            var hs = PooledHashSet<Symbol>.GetInstance();
            ClassDependsClosure(depends, depends.DeclaringCompilation, hs);

            var result = hs.Contains(on);
            hs.Free();

            return result;
        }

        private static void ClassDependsClosure(NamedTypeSymbol type, CSharpCompilation currentCompilation, HashSet<Symbol> partialClosure)
        {
            if ((object)type == null)
            {
                return;
            }

            type = type.OriginalDefinition;
            if (partialClosure.Add(type))
            {
                ClassDependsClosure(type.GetDeclaredBaseType(null), currentCompilation, partialClosure);

                // containment is interesting only for the current compilation
                if (currentCompilation != null && type.IsFromCompilation(currentCompilation))
                {
                    ClassDependsClosure(type.ContainingType, currentCompilation, partialClosure);
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
                    if ((object)field == null || field.Type.TypeKind != TypeKind.Struct || field.IsStatic)
                    {
                        continue;
                    }

                    StructDependsClosure((NamedTypeSymbol)field.Type, partialClosure, on);
                }
            }
        }

        /// <summary>
        /// IsManagedType is simple for most named types:
        ///     enums are not managed;
        ///     non-enum, non-struct named types are managed;
        ///     generic types and their nested types are managed;
        ///     type parameters are managed;
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
        internal static bool IsManagedType(NamedTypeSymbol type)
        {
            // If this is a type with an obvious answer, return quickly.
            switch (IsManagedTypeHelper(type))
            {
                case ThreeState.True:
                    return true;
                case ThreeState.False:
                    return false;
            }

            // Otherwise, we have to build and inspect the closure of depended-upon types.
            var hs = PooledHashSet<Symbol>.GetInstance();
            bool result = DependsOnDefinitelyManagedType(type, hs);
            hs.Free();
            return result;
        }

        private static bool DependsOnDefinitelyManagedType(NamedTypeSymbol type, HashSet<Symbol> partialClosure)
        {
            Debug.Assert((object)type != null);

            // NOTE: unlike in StructDependsClosure, we don't have to check for expanding cycles,
            // because as soon as we see something with non-zero arity we kick out (generic => managed).
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

                    // pointers are unmanaged
                    // NOTE: If we do not check HasPointerType, we will unconditionally
                    //       bind Type and that may cause infinite recursion.
                    //       HasPointerType can use syntax directly and break recursion.
                    if (field.HasPointerType)
                    {
                        continue;
                    }

                    TypeSymbol fieldType = field.Type;
                    NamedTypeSymbol fieldNamedType = fieldType as NamedTypeSymbol;
                    if ((object)fieldNamedType == null)
                    {
                        if (fieldType.IsManagedType)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // NOTE: don't use IsManagedType on a NamedTypeSymbol - that could lead
                        // to infinite recursion.
                        switch (IsManagedTypeHelper(fieldNamedType))
                        {
                            case ThreeState.True:
                                return true;
                            case ThreeState.False:
                                continue;
                            case ThreeState.Unknown:
                                if (DependsOnDefinitelyManagedType(fieldNamedType, partialClosure))
                                {
                                    return true;
                                }
                                continue;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a boolean value if we can determine whether the type is managed
        /// without looking at its fields and Unset otherwise.
        /// </summary>
        private static ThreeState IsManagedTypeHelper(NamedTypeSymbol type)
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
                    return ThreeState.False;
                case SpecialType.None:
                default:
                    // CONSIDER: could provide cases for other common special types.
                    break; // Proceed with additional checks.
            }

            if (type.AllTypeArgumentCount() > 0)
            {
                return ThreeState.True;
            }

            switch (type.TypeKind)
            {
                case TypeKind.Enum:
                    return ThreeState.False;
                case TypeKind.Struct:
                    return ThreeState.Unknown;
                default:
                    return ThreeState.True;
            }
        }

        internal static bool InterfaceDependsOn(NamedTypeSymbol depends, NamedTypeSymbol on)
        {
            Debug.Assert((object)depends != null);
            Debug.Assert((object)on != null);
            Debug.Assert(on.IsDefinition);

            var hs = PooledHashSet<Symbol>.GetInstance();
            InterfaceDependsClosure(depends, hs);

            var result = hs.Contains(on);
            hs.Free();

            return result;
        }

        private static void InterfaceDependsClosure(NamedTypeSymbol type, HashSet<Symbol> partialClosure)
        {
            type = type.OriginalDefinition;
            if (partialClosure.Add(type))
            {
                foreach (var bt in type.GetDeclaredInterfaces(null))
                {
                    InterfaceDependsClosure(bt, partialClosure);
                    // containment is not interesting for interfaces as they cannot nest in C#
                }
            }
        }
    }
}
