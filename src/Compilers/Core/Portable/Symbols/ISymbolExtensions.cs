// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    public static partial class ISymbolExtensions
    {
        /// <summary>
        /// Returns the constructed form of the ReducedFrom property,
        /// including the type arguments that were either inferred during reduction or supplied at the call site.
        /// </summary>
        public static IMethodSymbol GetConstructedReducedFrom(this IMethodSymbol method)
        {
            if (method.MethodKind != MethodKind.ReducedExtension)
            {
                // not a reduced extension method
                return null;
            }

            var reducedFrom = method.ReducedFrom;
            if (!reducedFrom.IsGenericMethod)
            {
                // not generic, no inferences were made
                return reducedFrom;
            }

            var typeArgs = new ITypeSymbol[reducedFrom.TypeParameters.Length];

            // first seed with any type arguments from reduced method
            for (int i = 0, n = method.TypeParameters.Length; i < n; i++)
            {
                var arg = method.TypeArguments[i];

                // make sure we don't construct with type parameters originating from reduced symbol.
                if (arg.Equals(method.TypeParameters[i]))
                {
                    arg = method.TypeParameters[i].ReducedFrom;
                }

                typeArgs[method.TypeParameters[i].ReducedFrom.Ordinal] = arg;
            }

            // add any inferences
            for (int i = 0, n = reducedFrom.TypeParameters.Length; i < n; i++)
            {
                var inferredType = method.GetTypeInferredDuringReduction(reducedFrom.TypeParameters[i]);
                if (inferredType != null)
                {
                    typeArgs[i] = inferredType;
                }
            }

            return reducedFrom.Construct(typeArgs);
        }

        /// <summary>
        /// Returns true if a given field is a nondefault tuple element
        /// </summary>
        internal static bool IsDefaultTupleElement(this IFieldSymbol field)
        {
            return (object)field == field.CorrespondingTupleField;
        }

        /// <summary>
        /// Returns true if a given field is a tuple element
        /// </summary>
        internal static bool IsTupleElement(this IFieldSymbol field)
        {
            return (object)field.CorrespondingTupleField != null;
        }

        /// <summary>
        /// Return the name of the field if the field is an explicitly named tuple element.
        /// Otherwise returns null.
        /// </summary>
        /// <remarks>
        /// Note that it is possible for an element to be both "Default" and to have a user provided name.
        /// That could happen if the provided name matches the default name such as "Item10"
        /// </remarks>
        internal static string ProvidedTupleElementNameOrNull(this IFieldSymbol field)
        {
            return field.IsTupleElement() && !field.IsImplicitlyDeclared ? field.Name : null;
        }

        internal static INamespaceSymbol GetNestedNamespace(this INamespaceSymbol container, string name)
        {
            foreach (var sym in container.GetMembers(name))
            {
                if (sym.Kind == SymbolKind.Namespace)
                {
                    return (INamespaceSymbol)sym;
                }
            }

            return null;
        }

        internal static bool IsNetModule(this IAssemblySymbol assembly) =>
            assembly is ISourceAssemblySymbol sourceAssembly && sourceAssembly.Compilation.Options.OutputKind.IsNetModule();

        internal static bool IsInSource(this ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.IsInSource)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
