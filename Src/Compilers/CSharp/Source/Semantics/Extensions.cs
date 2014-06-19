// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    using Metadata.PE;

    internal static partial class Extensions
    {
        public static ArrayTypeSymbol MakeArrayType(this TypeSymbol elementType)
        {
            return MakeArrayType(elementType, 1);
        }

        public static ArrayTypeSymbol MakeArrayType(this TypeSymbol elementType, int rank)
        {
            Debug.Assert(elementType != null);
            return new ArrayTypeSymbol(elementType, rank);
        }

        public static NamedTypeSymbol Construct(this NamedTypeSymbol type, params TypeSymbol[] arguments)
        {
            Debug.Assert(type != null);
            Debug.Assert(arguments != null);
            TypeMap map = new TypeMap(type.ConstructedFrom.TypeParameters, arguments);
            return map.SubstNamedType(type.ConstructedFrom);
        }

        public static bool IsNeverSameType(this TypeSymbol type)
        {
            return false;

            // UNDONE: for "types" like the lambda expression type.
        }

        public static TypeSymbol GetNullableUnderlyingType(this TypeSymbol type)
        {
            Debug.Assert(type != null);
            Debug.Assert(type is NamedTypeSymbol);
            return ((NamedTypeSymbol)type).TypeArguments[0];
        }

        public static IEnumerable<T> TransitiveClosure<T>(
            this Func<T, IEnumerable<T>> relation,
            T item)
        {
            var closure = new HashSet<T>();
            var stack = new Stack<T>();
            stack.Push(item);
            while (stack.Count > 0)
            {
                T current = stack.Pop();
                foreach (T newItem in relation(current))
                {
                    if (!closure.Contains(newItem))
                    {
                        closure.Add(newItem);
                        stack.Push(newItem);
                        yield return newItem;
                    }
                }
            }
        }

        public static bool DependsOn(this TypeParameterSymbol typeParameter1, TypeSymbol typeParameter2)
        {
            Debug.Assert(typeParameter1 != null);
            Debug.Assert(typeParameter2 != null);
            TypeParameterSymbol t2 = typeParameter2 as TypeParameterSymbol;
            if (t2 == null)
            {
                return false;
            }

            Func<TypeParameterSymbol, IEnumerable<TypeParameterSymbol>> dependencies = x => x.ConstraintTypes.OfType<TypeParameterSymbol>();
            return dependencies.TransitiveClosure(typeParameter1).Contains(t2);
        }

        public static bool IsEnumType(this TypeSymbol type)
        {
            Debug.Assert(type != null);
            return type.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)type).TypeKind == TypeKind.Enum;
        }

        public static bool IsInterfaceType(this TypeSymbol type)
        {
            Debug.Assert(type != null);
            return type.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)type).TypeKind == TypeKind.Interface;
        }

        public static bool IsClassType(this TypeSymbol type)
        {
            Debug.Assert(type != null);
            return type.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)type).TypeKind == TypeKind.Class;
        }

        public static bool IsErrorType(this TypeSymbol type)
        {
            Debug.Assert(type != null);
            return type.Kind == SymbolKind.ErrorType;
        }

        public static bool IsDelegateType(this TypeSymbol type)
        {
            Debug.Assert(type != null);
            return type.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)type).TypeKind == TypeKind.Delegate;
        }

        public static IEnumerable<NamedTypeSymbol> TypeAndOuterTypes(this NamedTypeSymbol type)
        {
            Debug.Assert(type != null);
            var cur = type;
            while (true)
            {
                if (cur == null)
                {
                    yield break;
                }

                yield return cur;
                cur = cur.ContainingType;
            }
        }

        // Given C<int>.D<string, double>, yields { int, string, double }
        public static IList<TypeSymbol> AllTypeArguments(this NamedTypeSymbol type)
        {
            Debug.Assert(type != null);
            var query =
                from t in type.TypeAndOuterTypes().Reverse()
                from a in t.TypeArguments
                select a;
            return query.ToList();
        }

        public static IEnumerable<NamedTypeSymbol> AllInterfaces(this NamedTypeSymbol type)
        {
            Debug.Assert(type != null);
            return from b in type.TypeAndBaseClasses()
                   from i in b.Interfaces.InterfacesAndBaseInterfaces()
                   select i;
        }

        public static IEnumerable<NamedTypeSymbol> TypeAndBaseClasses(this NamedTypeSymbol type)
        {
            Debug.Assert(type != null);
            var cur = type;
            while (cur != null)
            {
                yield return cur;
                cur = cur.BaseType;
            }
        }

        public static IEnumerable<NamedTypeSymbol> BaseClasses(this NamedTypeSymbol type)
        {
            return TypeAndBaseClasses(type).Skip(1);
        }

        public static NamedTypeSymbol GetEffectiveBaseClass(this TypeParameterSymbol type)
        {
            return type.BaseType;
        }

        // Takes a set of interfaces and returns all the interfaces, and the transitive closure of their base interfaces.
        public static IEnumerable<NamedTypeSymbol> InterfacesAndBaseInterfaces(this IEnumerable<NamedTypeSymbol> interfaces)
        {
            var stack = new Stack<NamedTypeSymbol>(interfaces);
            while (stack.Count != 0)
            {
                var i = stack.Pop();
                Debug.Assert(i.IsInterfaceType());
                yield return i;
                foreach (var b in i.Interfaces)
                {
                    stack.Push(b);
                }
            }
        }
    }
}