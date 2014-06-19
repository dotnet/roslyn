using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal static partial class NamedTypeSymbolExtensions
    {
        public static NamedTypeSymbol Construct(this NamedTypeSymbol type, TypeSymbol arguments)
        {
            Debug.Assert(type != null);
            Debug.Assert(arguments != null);
            TypeMap map = new TypeMap(ReadOnlyArray<TypeSymbol>.CreateFrom(type.ConstructedFrom.TypeParameters),
                                            ReadOnlyArray.Singleton(arguments));

            return map.SubstituteNamedType(type.ConstructedFrom);
        }
        
        public static NamedTypeSymbol Construct(this NamedTypeSymbol type, ReadOnlyArray<TypeSymbol> arguments)
        {
            Debug.Assert(type != null);
            Debug.Assert(arguments.IsNotNull);
            TypeMap map = new TypeMap(ReadOnlyArray<TypeSymbol>.CreateFrom(type.ConstructedFrom.TypeParameters),
                                            arguments);

            return map.SubstituteNamedType(type.ConstructedFrom);
        }

        public static NamedTypeSymbol Construct(this NamedTypeSymbol type, params TypeSymbol[] arguments)
        {
            Debug.Assert(type != null);
            Debug.Assert(arguments != null);
            TypeMap map = new TypeMap(ReadOnlyArray<TypeSymbol>.CreateFrom(type.ConstructedFrom.TypeParameters),
                                            arguments.AsReadOnly());

            return map.SubstituteNamedType(type.ConstructedFrom);
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

        public static bool HasTypeArguments(this NamedTypeSymbol type)
        {
            while (type != null)
            {
                if (type.TypeArguments.Count != 0)
                {
                    return true;
                }
                type = type.ContainingType;
            }
            return false;
        }

        // Given C<int>.D<string, double>, yields { int, string, double }
        public static void GetAllTypeArguments(this NamedTypeSymbol type, ArrayBuilder<TypeSymbol> builder)
        {
            var outer = type.ContainingType;
            if (outer != null)
            {
                outer.GetAllTypeArguments(builder);
            }

            builder.AddRange(type.TypeArguments);
        }

        public static int AllTypeArgumentCount(this NamedTypeSymbol type)
        {
            int count = type.TypeArguments.Count;

            var outer = type.ContainingType;
            if (outer != null)
            {
                count += outer.AllTypeArgumentCount();
            }
            return count;
        }

        public static TypeSymbol GetFirstTypeArgument(this NamedTypeSymbol type)
        {
            var outer = type.ContainingType;
            if (outer != null)
            {
                return outer.GetFirstTypeArgument();
            }
            return type.TypeArguments[0];
        }
        
        private static IEnumerable<NamedTypeSymbol> OuterAndBaseTypeDefinitions(this NamedTypeSymbol symbol)
        {
            if (symbol.ContainingType != null)
            {
                yield return (NamedTypeSymbol)symbol.ContainingType.OriginalDefinition;
            }

            if (symbol.BaseType != null)
            {
                yield return (NamedTypeSymbol)symbol.BaseType.OriginalDefinition;
            }
        }

        public static HashSet<NamedTypeSymbol> AllOuterTypeAndBaseTypeDefinitions(this NamedTypeSymbol symbol)
        {
            Func<NamedTypeSymbol, IEnumerable<NamedTypeSymbol>> relation = OuterAndBaseTypeDefinitions;
            return relation.TransitiveClosure(symbol);
        }

        public static HashSet<NamedTypeSymbol> TypeAndAllOuterTypeAndBaseTypeDefinitions(this NamedTypeSymbol symbol)
        {
            var result = symbol.AllOuterTypeAndBaseTypeDefinitions();
            result.Add(symbol);
            return result;
        }

        public static bool BaseClassDefinitionsContain(this NamedTypeSymbol type, Symbol other)
        {
            Debug.Assert(type != null);

            var cur = type.BaseType;
            while (cur != null)
            {
                if (cur == other)
                {
                    return true;
                }
                cur = cur.BaseType;
            }

            return false;
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

        public static IEnumerable<NamedTypeSymbol> InterfacesAndBaseInterfaces(this TypeSymbol type)
        {
            var stack = new Stack<NamedTypeSymbol>(type.Interfaces);
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
