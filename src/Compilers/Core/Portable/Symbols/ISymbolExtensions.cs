// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    using LockTypeInfo = (IMethodSymbol EnterScopeMethod, IMethodSymbol ScopeDisposeMethod);

    public static partial class ISymbolExtensions
    {
        /// <summary>
        /// Returns the constructed form of the ReducedFrom property,
        /// including the type arguments that were either inferred during reduction or supplied at the call site.
        /// </summary>
        public static IMethodSymbol? GetConstructedReducedFrom(this IMethodSymbol method)
        {
            if (method.MethodKind != MethodKind.ReducedExtension)
            {
                // not a reduced extension method
                return null;
            }

            var reducedFrom = method.ReducedFrom;
            Debug.Assert(reducedFrom is object);
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
                var typeParameter = method.TypeParameters[i];
                Debug.Assert(typeParameter.ReducedFrom is object);

                // make sure we don't construct with type parameters originating from reduced symbol.
                if (arg.Equals(typeParameter))
                {
                    arg = typeParameter.ReducedFrom;
                }

                typeArgs[typeParameter.ReducedFrom.Ordinal] = arg;
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
        /// Returns true if a given field is a default tuple element
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
            return field.CorrespondingTupleField is object;
        }

        /// <summary>
        /// Return the name of the field if the field is an explicitly named tuple element.
        /// Otherwise returns null.
        /// </summary>
        /// <remarks>
        /// Note that it is possible for an element to be both "Default" and to have a user provided name.
        /// That could happen if the provided name matches the default name such as "Item10"
        /// </remarks>
        internal static string? ProvidedTupleElementNameOrNull(this IFieldSymbol field)
        {
            return field.IsTupleElement() && !field.IsImplicitlyDeclared ? field.Name : null;
        }

        internal static INamespaceSymbol? GetNestedNamespace(this INamespaceSymbol container, string name)
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

        // Keep consistent with TypeSymbolExtensions.IsWellKnownTypeLock.
        internal static bool IsWellKnownTypeLock(this ITypeSymbol type)
        {
            return type is INamedTypeSymbol
            {
                Name: WellKnownMemberNames.LockTypeName,
                Arity: 0,
                ContainingType: null,
                ContainingNamespace:
                {
                    Name: nameof(System.Threading),
                    ContainingNamespace:
                    {
                        Name: nameof(System),
                        ContainingNamespace.IsGlobalNamespace: true,
                    }
                }
            };
        }

        // Keep consistent with LockBinder.TryFindLockTypeInfo.
        internal static LockTypeInfo? TryFindLockTypeInfo(this ITypeSymbol lockType)
        {
            IMethodSymbol? enterScopeMethod = TryFindPublicVoidParameterlessMethod(lockType, WellKnownMemberNames.EnterScopeMethodName);
            if (enterScopeMethod is not { ReturnsVoid: false, RefKind: RefKind.None })
            {
                return null;
            }

            ITypeSymbol? scopeType = enterScopeMethod.ReturnType;
            if (scopeType is not INamedTypeSymbol { Name: WellKnownMemberNames.LockScopeTypeName, Arity: 0, IsValueType: true, IsRefLikeType: true, DeclaredAccessibility: Accessibility.Public } ||
                !lockType.Equals(scopeType.ContainingType, SymbolEqualityComparer.ConsiderEverything))
            {
                return null;
            }

            IMethodSymbol? disposeMethod = TryFindPublicVoidParameterlessMethod(scopeType, WellKnownMemberNames.DisposeMethodName);
            if (disposeMethod is not { ReturnsVoid: true })
            {
                return null;
            }

            return new LockTypeInfo
            {
                EnterScopeMethod = enterScopeMethod,
                ScopeDisposeMethod = disposeMethod,
            };
        }

        // Keep consistent with LockBinder.TryFindPublicVoidParameterlessMethod.
        private static IMethodSymbol? TryFindPublicVoidParameterlessMethod(ITypeSymbol type, string name)
        {
            var members = type.GetMembers(name);
            IMethodSymbol? result = null;
            foreach (var member in members)
            {
                if (member is IMethodSymbol
                    {
                        Parameters: [],
                        Arity: 0,
                        IsStatic: false,
                        DeclaredAccessibility: Accessibility.Public,
                        MethodKind: MethodKind.Ordinary,
                    } method)
                {
                    if (result is not null)
                    {
                        // Ambiguous method found.
                        return null;
                    }

                    result = method;
                }
            }

            return result;
        }
    }
}
