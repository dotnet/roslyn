// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISymbolExtensions
    {
        public static string ToNameDisplayString(this ISymbol symbol)
            => symbol.ToDisplayString(SymbolDisplayFormats.NameFormat);

        public static string ToSignatureDisplayString(this ISymbol symbol)
            => symbol.ToDisplayString(SymbolDisplayFormats.SignatureFormat);

        public static bool HasPublicResultantVisibility(this ISymbol symbol)
            => symbol.GetResultantVisibility() == SymbolVisibility.Public;

        public static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
        {
            // Start by assuming it's visible.
            var visibility = SymbolVisibility.Public;

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    // Aliases are uber private.  They're only visible in the same file that they
                    // were declared in.
                    return SymbolVisibility.Private;

                case SymbolKind.Parameter:
                    // Parameters are only as visible as their containing symbol
                    return GetResultantVisibility(symbol.ContainingSymbol);

                case SymbolKind.TypeParameter:
                    // Type Parameters are private.
                    return SymbolVisibility.Private;
            }

            while (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    // If we see anything private, then the symbol is private.
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                        return SymbolVisibility.Private;

                    // If we see anything internal, then knock it down from public to
                    // internal.
                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        visibility = SymbolVisibility.Internal;
                        break;

                        // For anything else (Public, Protected, ProtectedOrInternal), the
                        // symbol stays at the level we've gotten so far.
                }

                symbol = symbol.ContainingSymbol;
            }

            return visibility;
        }

        public static ISymbol? GetOverriddenMember(this ISymbol? symbol)
            => symbol switch
            {
                IMethodSymbol method => method.OverriddenMethod,
                IPropertySymbol property => property.OverriddenProperty,
                IEventSymbol @event => @event.OverriddenEvent,
                _ => null,
            };

        public static ImmutableArray<ISymbol> ExplicitInterfaceImplementations(this ISymbol symbol)
            => symbol switch
            {
                IEventSymbol @event => ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations),
                IMethodSymbol method => ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations),
                IPropertySymbol property => ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations),
                _ => ImmutableArray.Create<ISymbol>(),
            };

        public static ImmutableArray<ISymbol> ExplicitOrImplicitInterfaceImplementations(this ISymbol symbol)
        {
            if (symbol.Kind is not SymbolKind.Method and not SymbolKind.Property and not SymbolKind.Event)
                return ImmutableArray<ISymbol>.Empty;

            var containingType = symbol.ContainingType;
            var query = from iface in containingType.AllInterfaces
                        from interfaceMember in iface.GetMembers()
                        let impl = containingType.FindImplementationForInterfaceMember(interfaceMember)
                        where symbol.Equals(impl)
                        select interfaceMember;
            return query.ToImmutableArray();
        }

        public static ImmutableArray<ISymbol> ImplicitInterfaceImplementations(this ISymbol symbol)
            => symbol.ExplicitOrImplicitInterfaceImplementations().Except(symbol.ExplicitInterfaceImplementations()).ToImmutableArray();

        public static bool IsOverridable([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            // Members can only have overrides if they are virtual, abstract or override and is not
            // sealed.
            return symbol?.ContainingType?.TypeKind == TypeKind.Class &&
                   (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) &&
                   !symbol.IsSealed;
        }

        public static bool IsImplementableMember([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            if (symbol != null &&
                symbol.ContainingType != null &&
                symbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                if (symbol.Kind == SymbolKind.Event)
                {
                    return true;
                }

                if (symbol.Kind == SymbolKind.Property)
                {
                    return true;
                }

                if (symbol.Kind == SymbolKind.Method)
                {
                    var methodSymbol = (IMethodSymbol)symbol;
                    if (methodSymbol.MethodKind is MethodKind.Ordinary or
                        MethodKind.PropertyGet or
                        MethodKind.PropertySet or
                        MethodKind.UserDefinedOperator or
                        MethodKind.Conversion)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static INamedTypeSymbol? GetContainingTypeOrThis(this ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return namedType;
            }

            return symbol.ContainingType;
        }

        public static bool IsPointerType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is IPointerTypeSymbol;

        public static bool IsErrorType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as ITypeSymbol)?.TypeKind == TypeKind.Error;

        public static bool IsModuleType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as ITypeSymbol)?.IsModuleType() == true;

        public static bool IsInterfaceType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as ITypeSymbol)?.IsInterfaceType() == true;

        public static bool IsArrayType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol?.Kind == SymbolKind.ArrayType;

        public static bool IsTupleType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as ITypeSymbol)?.IsTupleType ?? false;

        public static bool IsAnonymousFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind == MethodKind.AnonymousFunction;

        public static bool IsKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind)
            => symbol.MatchesKind(kind);

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind)
            => symbol?.Kind == kind;

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind1, SymbolKind kind2)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2);
        }

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind1, SymbolKind kind2, SymbolKind kind3)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2 || symbol.Kind == kind3);
        }

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, params SymbolKind[] kinds)
        {
            return symbol != null
                && kinds.Contains(symbol.Kind);
        }

        public static bool IsReducedExtension([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is IMethodSymbol method && method.MethodKind == MethodKind.ReducedExtension;

        public static bool IsEnumMember([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol?.Kind == SymbolKind.Field && symbol.ContainingType.IsEnumType();

        public static bool IsExtensionMethod(this ISymbol symbol)
            => symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).IsExtensionMethod;

        public static bool IsLocalFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol != null && symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.LocalFunction;

        public static bool IsAnonymousOrLocalFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol.IsAnonymousFunction() || symbol.IsLocalFunction();

        public static bool IsModuleMember([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol != null && symbol.ContainingSymbol is INamedTypeSymbol && symbol.ContainingType.TypeKind == TypeKind.Module;

        public static bool IsConstructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind == MethodKind.Constructor;

        public static bool IsStaticConstructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind == MethodKind.StaticConstructor;

        public static bool IsDestructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind == MethodKind.Destructor;

        public static bool IsUserDefinedOperator([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind == MethodKind.UserDefinedOperator;

        public static bool IsConversion([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind == MethodKind.Conversion;

        public static bool IsOrdinaryMethod([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind == MethodKind.Ordinary;

        public static bool IsOrdinaryMethodOrLocalFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            if (symbol is not IMethodSymbol method)
            {
                return false;
            }

            return method.MethodKind is MethodKind.Ordinary
                or MethodKind.LocalFunction;
        }

        public static bool IsDelegateType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is ITypeSymbol && ((ITypeSymbol)symbol).TypeKind == TypeKind.Delegate;

        public static bool IsAnonymousType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is INamedTypeSymbol && ((INamedTypeSymbol)symbol).IsAnonymousType;

        public static bool IsNormalAnonymousType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol.IsAnonymousType() && !symbol.IsDelegateType();

        public static bool IsAnonymousDelegateType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol.IsAnonymousType() && symbol.IsDelegateType();

        public static bool IsAnonymousTypeProperty([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is IPropertySymbol && symbol.ContainingType.IsNormalAnonymousType();

        public static bool IsTupleField([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is IFieldSymbol && symbol.ContainingType.IsTupleType;

        public static bool IsIndexer([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IPropertySymbol)?.IsIndexer == true;

        public static bool IsWriteableFieldOrProperty([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol switch
            {
                IFieldSymbol fieldSymbol => !fieldSymbol.IsReadOnly && !fieldSymbol.IsConst,
                IPropertySymbol propertySymbol => !propertySymbol.IsReadOnly,
                _ => false,
            };

        public static ITypeSymbol? GetMemberType(this ISymbol symbol)
            => symbol switch
            {
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IMethodSymbol methodSymbol => methodSymbol.ReturnType,
                IEventSymbol eventSymbol => eventSymbol.Type,
                _ => null,
            };

        public static int GetArity(this ISymbol symbol)
            => symbol.Kind switch
            {
                SymbolKind.NamedType => ((INamedTypeSymbol)symbol).Arity,
                SymbolKind.Method => ((IMethodSymbol)symbol).Arity,
                _ => 0,
            };

        [return: NotNullIfNotNull(parameterName: "symbol")]
        public static ISymbol? GetOriginalUnreducedDefinition(this ISymbol? symbol)
        {
            if (symbol.IsTupleField())
            {
                return symbol;
            }

            if (symbol.IsReducedExtension())
            {
                // note: ReducedFrom is only a method definition and includes no type arguments.
                symbol = ((IMethodSymbol)symbol).GetConstructedReducedFrom();
            }

            if (symbol.IsFunctionValue())
            {
                if (symbol.ContainingSymbol is IMethodSymbol method)
                {
                    symbol = method;

                    if (method.AssociatedSymbol != null)
                    {
                        symbol = method.AssociatedSymbol;
                    }
                }
            }

            if (symbol.IsNormalAnonymousType() || symbol.IsAnonymousTypeProperty())
            {
                return symbol;
            }

            if (symbol is IParameterSymbol parameter)
            {
                var method = parameter.ContainingSymbol as IMethodSymbol;
                if (method?.IsReducedExtension() == true)
                {
                    symbol = method.GetConstructedReducedFrom()!.Parameters[parameter.Ordinal + 1];
                }
            }

            return symbol?.OriginalDefinition;
        }

        public static bool IsFunctionValue([NotNullWhen(true)] this ISymbol? symbol)
            => symbol is ILocalSymbol { IsFunctionValue: true };

        public static bool IsThisParameter([NotNullWhen(true)] this ISymbol? symbol)
            => symbol is IParameterSymbol { IsThis: true };

        [return: NotNullIfNotNull(parameterName: "symbol")]
        public static ISymbol? ConvertThisParameterToType(this ISymbol? symbol)
        {
            if (symbol.IsThisParameter())
            {
                return ((IParameterSymbol)symbol).Type;
            }

            return symbol;
        }

        public static bool IsParams([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            var parameters = symbol.GetParameters();
            return parameters.Length > 0 && parameters[^1].IsParams;
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
            => symbol switch
            {
                IMethodSymbol m => m.Parameters,
                IPropertySymbol nt => nt.Parameters,
                _ => ImmutableArray<IParameterSymbol>.Empty,
            };

        public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol? symbol)
            => symbol switch
            {
                IMethodSymbol m => m.TypeParameters,
                INamedTypeSymbol nt => nt.TypeParameters,
                _ => ImmutableArray<ITypeParameterSymbol>.Empty,
            };

        public static ImmutableArray<ITypeParameterSymbol> GetAllTypeParameters(this ISymbol? symbol)
        {
            var results = ArrayBuilder<ITypeParameterSymbol>.GetInstance();

            while (symbol != null)
            {
                results.AddRange(symbol.GetTypeParameters());
                symbol = symbol.ContainingType;
            }

            return results.ToImmutableAndFree();
        }

        public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol? symbol)
            => symbol switch
            {
                IMethodSymbol m => m.TypeArguments,
                INamedTypeSymbol nt => nt.TypeArguments,
                _ => ImmutableArray.Create<ITypeSymbol>(),
            };

        public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this ISymbol symbol)
        {
            var results = ArrayBuilder<ITypeSymbol>.GetInstance();
            results.AddRange(symbol.GetTypeArguments());

            var containingType = symbol.ContainingType;
            while (containingType != null)
            {
                results.AddRange(containingType.GetTypeArguments());
                containingType = containingType.ContainingType;
            }

            return results.ToImmutableAndFree();
        }

        public static bool IsAttribute([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as ITypeSymbol)?.IsAttribute() == true;

        /// <summary>
        /// Returns <see langword="true"/> if the signature of this symbol requires the <see
        /// langword="unsafe"/> modifier.  For example a method that takes <c>List&lt;int*[]&gt;</c>
        /// is unsafe, as is <c>int* Goo { get; }</c>.  This will return <see langword="false"/> for
        /// symbols that cannot have the <see langword="unsafe"/> modifier on them.
        /// </summary>
        public static bool RequiresUnsafeModifier([NotNullWhen(returnValue: true)] this ISymbol? member)
        {
            // TODO(cyrusn): Defer to compiler code to handle this once it can.
            return member?.Accept(new RequiresUnsafeModifierVisitor()) == true;
        }

        public static ITypeSymbol ConvertToType(
            this ISymbol? symbol,
            Compilation compilation,
            bool extensionUsedAsInstance = false)
        {
            if (symbol is ITypeSymbol type)
            {
                return type;
            }

            if (symbol is IMethodSymbol method && method.Parameters.All(p => p.RefKind == RefKind.None))
            {
                var count = extensionUsedAsInstance ? Math.Max(0, method.Parameters.Length - 1) : method.Parameters.Length;
                var skip = extensionUsedAsInstance ? 1 : 0;

                // Convert the symbol to Func<...> or Action<...>
                var delegateType = compilation.GetTypeByMetadataName(method.ReturnsVoid
                    ? WithArity("System.Action", count)
                    : WithArity("System.Func", count + 1));

                if (delegateType != null)
                {
                    var types = method.Parameters
                        .Skip(skip)
                        .Select(p => (p.Type ?? compilation.GetSpecialType(SpecialType.System_Object)).WithNullableAnnotation(p.NullableAnnotation));

                    if (!method.ReturnsVoid)
                    {
                        // +1 for the return type.
                        types = types.Concat((method.ReturnType ?? compilation.GetSpecialType(SpecialType.System_Object)).WithNullableAnnotation(method.ReturnNullableAnnotation));
                    }

                    return delegateType.TryConstruct(types.ToArray());
                }
            }

            // Otherwise, just default to object.
            return compilation.ObjectType;

            // local functions
            static string WithArity(string typeName, int arity)
                => arity > 0 ? typeName + '`' + arity : typeName;
        }

        public static bool IsStaticType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol != null && symbol.Kind == SymbolKind.NamedType && symbol.IsStatic;

        public static bool IsNamespace([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol?.Kind == SymbolKind.Namespace;

        public static bool IsOrContainsAccessibleAttribute(
            [NotNullWhen(returnValue: true)] this ISymbol? symbol, ISymbol withinType, IAssemblySymbol withinAssembly, CancellationToken cancellationToken)
        {
            var namespaceOrType = symbol is IAliasSymbol alias ? alias.Target : symbol as INamespaceOrTypeSymbol;
            if (namespaceOrType == null)
            {
                return false;
            }

            // PERF: Avoid allocating a lambda capture
            foreach (var type in namespaceOrType.GetAllTypes(cancellationToken))
            {
                if (type.IsAttribute() && type.IsAccessibleWithin(withinType ?? withinAssembly))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IPropertySymbol> GetValidAnonymousTypeProperties(this ISymbol symbol)
        {
            Contract.ThrowIfFalse(symbol.IsNormalAnonymousType());
            return ((INamedTypeSymbol)symbol).GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName);
        }

        public static Accessibility ComputeResultantAccessibility(this ISymbol? symbol, ITypeSymbol finalDestination)
        {
            if (symbol == null)
            {
                return Accessibility.Private;
            }

            switch (symbol.DeclaredAccessibility)
            {
                default:
                    return symbol.DeclaredAccessibility;
                case Accessibility.ProtectedAndInternal:
                    return symbol.ContainingAssembly.GivesAccessTo(finalDestination.ContainingAssembly)
                        ? Accessibility.ProtectedAndInternal
                        : Accessibility.Internal;
                case Accessibility.ProtectedOrInternal:
                    return symbol.ContainingAssembly.GivesAccessTo(finalDestination.ContainingAssembly)
                        ? Accessibility.ProtectedOrInternal
                        : Accessibility.Protected;
            }
        }

        /// <returns>
        /// Returns true if symbol is a local variable and its declaring syntax node is 
        /// after the current position, false otherwise (including for non-local symbols)
        /// </returns>
        public static bool IsInaccessibleLocal(this ISymbol symbol, int position)
        {
            if (symbol.Kind != SymbolKind.Local)
            {
                return false;
            }

            // Implicitly declared locals (with Option Explicit Off in VB) are scoped to the entire
            // method and should always be considered accessible from within the same method.
            if (symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            var declarationSyntax = symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).FirstOrDefault();
            return declarationSyntax != null && position < declarationSyntax.SpanStart;
        }

        public static bool IsAccessor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol.IsPropertyAccessor() || symbol.IsEventAccessor();

        public static bool IsPropertyAccessor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.MethodKind.IsPropertyAccessor() == true;

        public static bool IsEventAccessor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            var method = symbol as IMethodSymbol;
            return method != null &&
                (method.MethodKind == MethodKind.EventAdd ||
                 method.MethodKind == MethodKind.EventRaise ||
                 method.MethodKind == MethodKind.EventRemove);
        }

        public static bool IsFromSource(this ISymbol symbol)
            => symbol.Locations.Any() && symbol.Locations.All(location => location.IsInSource);

        public static bool IsNonImplicitAndFromSource(this ISymbol symbol)
            => !symbol.IsImplicitlyDeclared && symbol.IsFromSource();

        public static ITypeSymbol? GetSymbolType(this ISymbol? symbol)
            => symbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IParameterSymbol parameterSymbol => parameterSymbol.Type,
                IAliasSymbol aliasSymbol => aliasSymbol.Target as ITypeSymbol,
                _ => symbol as ITypeSymbol,
            };

        /// <summary>
        /// If the <paramref name="symbol"/> is a method symbol, returns <see langword="true"/> if the method's return type is "awaitable", but not if it's <see langword="dynamic"/>.
        /// If the <paramref name="symbol"/> is a type symbol, returns <see langword="true"/> if that type is "awaitable".
        /// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter". This GetAwaiter method may be an instance method or an extension method.
        /// </summary>
        public static bool IsAwaitableNonDynamic([NotNullWhen(returnValue: true)] this ISymbol? symbol, SemanticModel semanticModel, int position)
        {
            var methodSymbol = symbol as IMethodSymbol;
            ITypeSymbol? typeSymbol = null;

            if (methodSymbol == null)
            {
                typeSymbol = symbol as ITypeSymbol;
                if (typeSymbol == null)
                {
                    return false;
                }
            }
            else
            {
                if (methodSymbol.ReturnType == null)
                {
                    return false;
                }
            }

            // otherwise: needs valid GetAwaiter
            var potentialGetAwaiters = semanticModel.LookupSymbols(position,
                                                                   container: typeSymbol ?? methodSymbol!.ReturnType.OriginalDefinition,
                                                                   name: WellKnownMemberNames.GetAwaiter,
                                                                   includeReducedExtensionMethods: true);
            var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
            return getAwaiters.Any(VerifyGetAwaiter);
        }

        public static bool IsValidGetAwaiter(this IMethodSymbol symbol)
            => symbol.Name == WellKnownMemberNames.GetAwaiter &&
            VerifyGetAwaiter(symbol);

        private static bool VerifyGetAwaiter(IMethodSymbol getAwaiter)
        {
            var returnType = getAwaiter.ReturnType;
            if (returnType == null)
            {
                return false;
            }

            // bool IsCompleted { get }
            if (!returnType.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name == WellKnownMemberNames.IsCompleted && p.Type.SpecialType == SpecialType.System_Boolean && p.GetMethod != null))
            {
                return false;
            }

            var methods = returnType.GetMembers().OfType<IMethodSymbol>();

            // NOTE: (vladres) The current version of C# Spec, §7.7.7.3 'Runtime evaluation of await expressions', requires that
            // NOTE: the interface method INotifyCompletion.OnCompleted or ICriticalNotifyCompletion.UnsafeOnCompleted is invoked
            // NOTE: (rather than any OnCompleted method conforming to a certain pattern).
            // NOTE: Should this code be updated to match the spec?

            // void OnCompleted(Action) 
            // Actions are delegates, so we'll just check for delegates.
            if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid && x.Parameters.Length == 1 && x.Parameters.First().Type.TypeKind == TypeKind.Delegate))
            {
                return false;
            }

            // void GetResult() || T GetResult()
            return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
        }

        public static bool IsValidGetEnumerator(this IMethodSymbol symbol)
            => symbol.Name == WellKnownMemberNames.GetEnumeratorMethodName &&
               VerifyGetEnumerator(symbol);

        private static bool VerifyGetEnumerator(IMethodSymbol getEnumerator)
        {
            var returnType = getEnumerator.ReturnType;
            if (returnType == null)
            {
                return false;
            }

            var members = returnType.AllInterfaces.Concat(returnType.GetBaseTypesAndThis())
                .SelectMany(x => x.GetMembers())
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            // T Current { get }
            if (!members.OfType<IPropertySymbol>().Any(p => p.Name == WellKnownMemberNames.CurrentPropertyName && p.GetMethod != null))
            {
                return false;
            }

            // bool MoveNext()
            if (!members.OfType<IMethodSymbol>().Any(x =>
            {
                return x is
                {
                    Name: WellKnownMemberNames.MoveNextMethodName,
                    ReturnType.SpecialType: SpecialType.System_Boolean,
                    Parameters.Length: 0,
                };
            }))
            {
                return false;
            }

            return true;
        }

        public static bool IsValidGetAsyncEnumerator(this IMethodSymbol symbol)
            => symbol.Name == WellKnownMemberNames.GetAsyncEnumeratorMethodName &&
                VerifyGetAsyncEnumerator(symbol);

        private static bool VerifyGetAsyncEnumerator(IMethodSymbol getAsyncEnumerator)
        {
            var returnType = getAsyncEnumerator.ReturnType;
            if (returnType == null)
            {
                return false;
            }

            var members = returnType.AllInterfaces.Concat(returnType.GetBaseTypesAndThis())
                .SelectMany(x => x.GetMembers())
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            // T Current { get }
            if (!members.OfType<IPropertySymbol>().Any(p => p.Name == WellKnownMemberNames.CurrentPropertyName && p.GetMethod != null))
            {
                return false;
            }

            // Task<bool> MoveNext()
            // We don't check for the return type, since it can be any awaitable wrapping a boolean, 
            // which is too complex to be worth checking here.
            // We don't check number of parameters since MoveNextAsync allows optional parameters/params
            if (!members.OfType<IMethodSymbol>().Any(x => x.Name == WellKnownMemberNames.MoveNextAsyncMethodName))
            {
                return false;
            }

            return true;
        }

        public static bool IsKind<TSymbol>(this ISymbol symbol, SymbolKind kind, [NotNullWhen(true)] out TSymbol? result) where TSymbol : class, ISymbol
        {
            if (!symbol.IsKind(kind))
            {
                result = null;
                return false;
            }

            result = (TSymbol)symbol;
            return true;
        }

        /// <summary>
        /// Returns true for symbols whose name starts with an underscore and
        /// are optionally followed by an integer, such as '_', '_1', '_2', etc.
        /// These are treated as special discard symbol names.
        /// </summary>
        public static bool IsSymbolWithSpecialDiscardName(this ISymbol symbol)
            => symbol.Name.StartsWith("_") &&
               (symbol.Name.Length == 1 || uint.TryParse(symbol.Name[1..], out _));

        /// <summary>
        /// Returns <see langword="true"/>, if the symbol is marked with the <see cref="System.ObsoleteAttribute"/>.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns><see langword="true"/> if the symbol is marked with the <see cref="System.ObsoleteAttribute"/>.</returns>
        public static bool IsObsolete(this ISymbol symbol)
            => symbol.GetAttributes().Any(x => x.AttributeClass is
            {
                MetadataName: nameof(ObsoleteAttribute),
                ContainingNamespace.Name: nameof(System),
            });
    }
}
