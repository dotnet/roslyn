// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
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

            var hs = PooledHashSet<NamedTypeSymbol>.GetInstance();
            var typesWithCycle = PooledHashSet<NamedTypeSymbol>.GetInstance();
            StructDependsClosure(depends, hs, typesWithCycle, ConsList<NamedTypeSymbol>.Empty.Prepend(on));

            var result = typesWithCycle.Contains(on);
            typesWithCycle.Free();
            hs.Free();

            return result;
        }

        private static void StructDependsClosure(NamedTypeSymbol type, HashSet<NamedTypeSymbol> partialClosure, HashSet<NamedTypeSymbol> typesWithCycle, ConsList<NamedTypeSymbol> on)
        {
            Debug.Assert((object)type != null);

            if (typesWithCycle.Contains(type.OriginalDefinition))
            {
                return;
            }

            if (on.ContainsReference(type.OriginalDefinition))
            {
                // found a possibly expanding cycle, for example
                //     struct X<T> { public T t; }
                //     struct W<T> { X<W<W<T>>> x; }
                // while not explicitly forbidden by the spec, it should be.
                typesWithCycle.Add(type.OriginalDefinition);
                return;
            }

            if (partialClosure.Add(type))
            {
                if (!type.IsDefinition)
                {
                    // First, visit type as a definition in order to detect the fact that it itself has a cycle.
                    // This prevents us from going into an infinite generic expansion while visiting constructed form
                    // of the type below.
                    visitFields(type.OriginalDefinition, partialClosure, typesWithCycle, on.Prepend(type.OriginalDefinition));
                }

                visitFields(type, partialClosure, typesWithCycle, on);
            }

            static void visitFields(NamedTypeSymbol type, HashSet<NamedTypeSymbol> partialClosure, HashSet<NamedTypeSymbol> typesWithCycle, ConsList<NamedTypeSymbol> on)
            {
                foreach (var member in type.GetMembersUnordered())
                {
                    var field = member as FieldSymbol;
                    var fieldType = field?.NonPointerType();
                    if (fieldType is null || fieldType.TypeKind != TypeKind.Struct || field.IsStatic)
                    {
                        continue;
                    }

                    StructDependsClosure((NamedTypeSymbol)fieldType, partialClosure, typesWithCycle, on);
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
        /// if one of its instance fields is managed or a ref field.  Unfortunately, this can result in infinite recursion.
        /// If the closure is finite, and we don't find anything definitely managed, then we return true.
        /// If the closure is infinite, we disregard all but a representative of any expanding cycle.
        /// 
        /// Intuitively, this will only return true if there's a specific type we can point to that is would
        /// be managed even if it had no fields.  e.g. struct S { S s; } is not managed, but struct S { S s; object o; }
        /// is because we can point to object.
        /// </summary>
        internal static ManagedKind GetManagedKind(NamedTypeSymbol type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // The code below should be kept in sync with NamedTypeSymbol.GetManagedKind in VB

            var (isManaged, hasGenerics) = INamedTypeSymbolInternal.Helpers.IsManagedTypeHelper(type);
            var definitelyManaged = isManaged == ThreeState.True;
            if (isManaged == ThreeState.Unknown)
            {
                // Otherwise, we have to build and inspect the closure of depended-upon types.
                var hs = PooledHashSet<Symbol>.GetInstance();
                var result = dependsOnDefinitelyManagedType(type, hs, ref useSiteInfo);
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

            static (bool definitelyManaged, bool hasGenerics) dependsOnDefinitelyManagedType(NamedTypeSymbol type, HashSet<Symbol> partialClosure, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
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

                        if (field.RefKind != RefKind.None)
                        {
                            // A ref struct which has a ref field is never considered unmanaged
                            return (true, hasGenerics);
                        }

                        TypeSymbol fieldType = field.NonPointerType();
                        if (fieldType is null)
                        {
                            // pointers are unmanaged
                            continue;
                        }

                        fieldType.AddUseSiteInfo(ref useSiteInfo);
                        NamedTypeSymbol fieldNamedType = fieldType as NamedTypeSymbol;
                        if ((object)fieldNamedType == null)
                        {
                            if (fieldType.IsManagedType(ref useSiteInfo))
                            {
                                return (true, hasGenerics);
                            }
                        }
                        else
                        {
                            var result = INamedTypeSymbolInternal.Helpers.IsManagedTypeHelper(fieldNamedType);
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
                                        var (definitelyManaged, childHasGenerics) = dependsOnDefinitelyManagedType(fieldNamedType, partialClosure, ref useSiteInfo);
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
        }

        internal static TypeSymbol NonPointerType(this FieldSymbol field) =>
            field.HasPointerType ? null : field.Type;
    }
}
