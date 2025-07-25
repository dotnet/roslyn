// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable RS1024 // Use 'SymbolEqualityComparer' when comparing symbols (https://github.com/dotnet/roslyn/issues/78583)
#pragma warning disable CS0419 // Ambiguous reference in cref attribute

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions
{
    extension(ISymbol symbol)
    {
        public string ToNameDisplayString()
        => symbol.ToDisplayString(SymbolDisplayFormats.NameFormat);

        public string ToSignatureDisplayString()
            => symbol.ToDisplayString(SymbolDisplayFormats.SignatureFormat);

        public bool HasPublicResultantVisibility()
            => symbol.GetResultantVisibility() == SymbolVisibility.Public;

        public SymbolVisibility GetResultantVisibility()
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

        public ImmutableArray<ISymbol> ExplicitInterfaceImplementations()
            => symbol switch
            {
                IEventSymbol @event => ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations),
                IMethodSymbol method => ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations),
                IPropertySymbol property => ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations),
                _ => [],
            };

        public ImmutableArray<ISymbol> ExplicitOrImplicitInterfaceImplementations()
        {
            if (symbol.Kind is not SymbolKind.Method and not SymbolKind.Property and not SymbolKind.Event)
                return [];

            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);

            foreach (var iface in symbol.ContainingType.AllInterfaces)
            {
                foreach (var interfaceMember in iface.GetMembers())
                {
                    var impl = symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                    if (symbol.Equals(impl))
                        result.Add(interfaceMember);
                }
            }

            // There are explicit methods that FindImplementationForInterfaceMember.  For exampl `abstract explicit impls`
            // like `abstract void I<T>.M()`.  So add these back in directly using symbol.ExplicitInterfaceImplementations.
            result.AddRange(symbol.ExplicitInterfaceImplementations());
            result.RemoveDuplicates();

            return result.ToImmutableAndClear();
        }

        public ImmutableArray<ISymbol> ImplicitInterfaceImplementations()
            => [.. symbol.ExplicitOrImplicitInterfaceImplementations().Except(symbol.ExplicitInterfaceImplementations())];

        public INamedTypeSymbol? GetContainingTypeOrThis()
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return namedType;
            }

            return symbol.ContainingType;
        }

        public bool IsExtensionMethod()
            => symbol is IMethodSymbol { IsExtensionMethod: true };

        public int GetArity()
            => symbol.Kind switch
            {
                SymbolKind.NamedType => ((INamedTypeSymbol)symbol).Arity,
                SymbolKind.Method => ((IMethodSymbol)symbol).Arity,
                _ => 0,
            };

        public ImmutableArray<ITypeSymbol> GetAllTypeArguments()
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

        public IEnumerable<IPropertySymbol> GetValidAnonymousTypeProperties()
        {
            Contract.ThrowIfFalse(symbol.IsNormalAnonymousType());
            return ((INamedTypeSymbol)symbol).GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName);
        }

        /// <returns>
        /// Returns true if symbol is a local variable and its declaring syntax node is 
        /// after the current position, false otherwise (including for non-local symbols)
        /// </returns>
        public bool IsInaccessibleLocal(int position)
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

        public bool IsFromSource()
            => symbol.Locations.Any() && symbol.Locations.All(location => location.IsInSource);

        public bool IsNonImplicitAndFromSource()
            => !symbol.IsImplicitlyDeclared && symbol.IsFromSource();

        public bool IsKind<TSymbol>(SymbolKind kind, [NotNullWhen(true)] out TSymbol? result) where TSymbol : class, ISymbol
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
        /// are optionally followed by an integer or other underscores, such as '_', '_1', '_2', '__', '___', etc.
        /// These are treated as special discard symbol names.
        /// </summary>
        public bool IsSymbolWithSpecialDiscardName()
            => symbol.Name.StartsWith("_") &&
               (symbol.Name.Length == 1 || uint.TryParse(symbol.Name[1..], out _) || symbol.Name.All(n => n.Equals('_')));

        /// <summary>
        /// Returns <see langword="true"/>, if the symbol is marked with the <see cref="ObsoleteAttribute"/> and does
        /// <em>not</em> have the <see cref="CompilerFeatureRequiredAttribute"/> attribute.
        /// </summary>
        /// <remarks>
        /// The compiler will emit ObsoleteAttributes on symbols along with CompilerFeatureRequiredAttribute to indicate
        /// that the symbol is conditionally obsolete, depending on if the target compiler supports that particular
        /// feature or not.  This information is then used just to tell the user a specific message, it is not actually
        /// intended to indicate the symbol is actually obsolete.  As such, we return 'false' here as this check is 
        /// intended for use by features that use the traditional concept of an actual <c>[Obsolete]</c> attribute added
        /// in source by the user themselves.
        /// </remarks>
        public bool IsObsolete()
        {
            if (symbol.GetAttributes().Any(static x => x.AttributeClass is
                {
                    MetadataName: nameof(ObsoleteAttribute),
                    ContainingNamespace.Name: nameof(System),
                    ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true,
                }))
            {
                if (!symbol.GetAttributes().Any(static x => x.AttributeClass is
                    {
                        MetadataName: nameof(CompilerFeatureRequiredAttribute),
                        ContainingNamespace.Name: nameof(System.Runtime.CompilerServices),
                        ContainingNamespace.ContainingNamespace.Name: nameof(System.Runtime),
                        ContainingNamespace.ContainingNamespace.ContainingNamespace.Name: nameof(System),
                        ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true,
                    }))
                {
                    return true;
                }
            }

            return false;
        }
    }

    extension(ISymbol? symbol)
    {
        public ISymbol? GetOverriddenMember(bool allowLooseMatch = false)
        {
            if (symbol is null)
                return null;

            ISymbol? exactMatch = symbol switch
            {
                IMethodSymbol method => method.OverriddenMethod,
                IPropertySymbol property => property.OverriddenProperty,
                IEventSymbol @event => @event.OverriddenEvent,
                _ => null,
            };

            if (exactMatch != null)
                return exactMatch;

            if (allowLooseMatch &&
                (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride))
            {
                foreach (var baseType in symbol.ContainingType.GetBaseTypes())
                {
                    if (TryFindLooseMatch(symbol, baseType, out var looseMatch))
                        return looseMatch;
                }
            }

            return null;

            bool TryFindLooseMatch(ISymbol symbol, INamedTypeSymbol baseType, [NotNullWhen(true)] out ISymbol? looseMatch)
            {
                IMethodSymbol? bestMethod = null;
                var parameterCount = symbol.GetParameters().Length;

                foreach (var member in baseType.GetMembers(symbol.Name))
                {
                    if (member.Kind != symbol.Kind)
                        continue;

                    if (!member.IsOverridable())
                        continue;

                    if (symbol.Kind is SymbolKind.Event or SymbolKind.Property)
                    {
                        // We've found a matching event/property in the base type (perhaps differing by return type). This
                        // is a good enough match to return as a loose match for the starting symbol.
                        looseMatch = member;
                        return true;
                    }
                    else if (member is IMethodSymbol method)
                    {
                        // Prefer methods that are closed in parameter count to the original method we started with.
                        if (bestMethod is null || Math.Abs(method.Parameters.Length - parameterCount) < Math.Abs(bestMethod.Parameters.Length - parameterCount))
                            bestMethod = method;
                    }
                }

                looseMatch = bestMethod;
                return looseMatch != null;
            }
        }

        public ITypeSymbol? GetMemberType()
            => symbol switch
            {
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IMethodSymbol methodSymbol => methodSymbol.ReturnType,
                IEventSymbol eventSymbol => eventSymbol.Type,
                IParameterSymbol parameterSymbol => parameterSymbol.Type,
                ILocalSymbol localSymbol => localSymbol.Type,
                _ => null,
            };

        [return: NotNullIfNotNull(parameterName: nameof(symbol))]
        public ISymbol? GetOriginalUnreducedDefinition()
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

        [return: NotNullIfNotNull(parameterName: nameof(symbol))]
        public ISymbol? ConvertThisParameterToType()
        {
            if (symbol.IsThisParameter())
            {
                return ((IParameterSymbol)symbol).Type;
            }

            return symbol;
        }

        public ImmutableArray<IParameterSymbol> GetParameters()
            => symbol switch
            {
                IMethodSymbol m => m.Parameters,
                IPropertySymbol nt => nt.Parameters,
                _ => [],
            };

        public ImmutableArray<ITypeParameterSymbol> GetTypeParameters()
            => symbol switch
            {
                IMethodSymbol m => m.TypeParameters,
                INamedTypeSymbol nt => nt.TypeParameters,
                _ => [],
            };

        public ImmutableArray<ITypeParameterSymbol> GetAllTypeParameters()
        {
            var results = ArrayBuilder<ITypeParameterSymbol>.GetInstance();

            while (symbol != null)
            {
                results.AddRange(symbol.GetTypeParameters());
                symbol = symbol.ContainingType;
            }

            return results.ToImmutableAndFree();
        }

        public ImmutableArray<ITypeSymbol> GetTypeArguments()
            => symbol switch
            {
                IMethodSymbol m => m.TypeArguments,
                INamedTypeSymbol nt => nt.TypeArguments,
                _ => [],
            };

        public ITypeSymbol ConvertToType(
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

                    return delegateType.TryConstruct([.. types]);
                }
            }

            // Otherwise, just default to object.
            return compilation.ObjectType;

            // local functions
            static string WithArity(string typeName, int arity)
                => arity > 0 ? typeName + '`' + arity : typeName;
        }

        public Accessibility ComputeResultantAccessibility(ITypeSymbol finalDestination)
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

        public ITypeSymbol? GetSymbolType()
            => symbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IParameterSymbol parameterSymbol => parameterSymbol.Type,
                IAliasSymbol aliasSymbol => aliasSymbol.Target as ITypeSymbol,
                _ => symbol as ITypeSymbol,
            };
    }

    extension([NotNullWhen(true)] ISymbol? symbol)
    {
        public bool IsOverridable()
        {
            // Members can only have overrides if they are virtual, abstract or override and is not sealed.
            return symbol is { ContainingType.TypeKind: TypeKind.Class, IsSealed: false } &&
                   (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride);
        }

        public bool IsImplementableMember()
        {
            if (symbol is { ContainingType.TypeKind: TypeKind.Interface })
            {
                if (symbol.Kind == SymbolKind.Event)
                {
                    return true;
                }

                if (symbol.Kind == SymbolKind.Property)
                {
                    return true;
                }

                if (symbol is IMethodSymbol
                    {
                        MethodKind: MethodKind.Ordinary or
                            MethodKind.PropertyGet or
                            MethodKind.PropertySet or
                            MethodKind.UserDefinedOperator or
                            MethodKind.Conversion
                    })
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsErrorType()
            => symbol is ITypeSymbol { TypeKind: TypeKind.Error };

        public bool IsModuleType()
            => symbol is ITypeSymbol { TypeKind: TypeKind.Module };

        public bool IsInterfaceType()
            => symbol is ITypeSymbol { TypeKind: TypeKind.Interface };

        public bool IsArrayType()
            => symbol is { Kind: SymbolKind.ArrayType };

        public bool IsTupleType()
            => symbol is ITypeSymbol { IsTupleType: true };

        public bool IsAnonymousFunction()
            => symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction };

        public bool IsKind(SymbolKind kind)
            => symbol.MatchesKind(kind);

        public bool MatchesKind(SymbolKind kind)
            => symbol?.Kind == kind;

        public bool MatchesKind(SymbolKind kind1, SymbolKind kind2)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2);
        }

        public bool MatchesKind(SymbolKind kind1, SymbolKind kind2, SymbolKind kind3)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2 || symbol.Kind == kind3);
        }

        public bool MatchesKind(params SymbolKind[] kinds)
        {
            return symbol != null
                && kinds.Contains(symbol.Kind);
        }

        public bool IsReducedExtension()
            => symbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension };

        public bool IsEnumMember()
            => symbol is { Kind: SymbolKind.Field, ContainingType.TypeKind: TypeKind.Enum };

        public bool IsLocalFunction()
            => symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction };

        public bool IsAnonymousOrLocalFunction()
            => symbol.IsAnonymousFunction() || symbol.IsLocalFunction();

        public bool IsModuleMember()
            => symbol is { ContainingType.TypeKind: TypeKind.Module };

        public bool IsConstructor()
            => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor };

        public bool IsStaticConstructor()
            => symbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor };

        public bool IsDestructor()
            => symbol is IMethodSymbol { MethodKind: MethodKind.Destructor };

        public bool IsUserDefinedOperator()
            => symbol is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator };

        public bool IsConversion()
            => symbol is IMethodSymbol { MethodKind: MethodKind.Conversion };

        public bool IsOrdinaryMethod()
            => symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary };

        public bool IsOrdinaryMethodOrLocalFunction()
            => symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.LocalFunction };

        public bool IsDelegateType()
            => symbol is ITypeSymbol { TypeKind: TypeKind.Delegate };

        public bool IsAnonymousType()
            => symbol is INamedTypeSymbol { IsAnonymousType: true };

        public bool IsNormalAnonymousType()
            => symbol.IsAnonymousType() && !symbol.IsDelegateType();

        public bool IsAnonymousDelegateType()
            => symbol.IsAnonymousType() && symbol.IsDelegateType();

        public bool IsAnonymousTypeProperty()
            => symbol is IPropertySymbol && symbol.ContainingType.IsNormalAnonymousType();

        public bool IsTupleField()
            => symbol is IFieldSymbol { ContainingType.IsTupleType: true };

        public bool IsIndexer()
            => symbol is IPropertySymbol { IsIndexer: true };

        public bool IsWriteableFieldOrProperty()
            => symbol switch
            {
                IFieldSymbol fieldSymbol => !fieldSymbol.IsReadOnly && !fieldSymbol.IsConst,
                IPropertySymbol propertySymbol => !propertySymbol.IsReadOnly,
                _ => false,
            };

        public bool IsRequired()
            => symbol is IFieldSymbol { IsRequired: true } or IPropertySymbol { IsRequired: true };

        public bool IsFunctionValue()
            => symbol is ILocalSymbol { IsFunctionValue: true };

        public bool IsThisParameter()
            => symbol is IParameterSymbol { IsThis: true };

        public bool IsParams()
            => symbol.GetParameters() is [.., { IsParams: true }];

        public bool IsAttribute()
            => (symbol as ITypeSymbol)?.IsAttribute() == true;

        public bool IsStaticType()
            => symbol is INamedTypeSymbol { IsStatic: true };

        public bool IsOrContainsAccessibleAttribute(
    ISymbol withinType, IAssemblySymbol withinAssembly, CancellationToken cancellationToken)
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

        public bool IsAccessor()
            => symbol.IsPropertyAccessor() || symbol.IsEventAccessor();

        public bool IsPropertyAccessor()
            => (symbol as IMethodSymbol)?.MethodKind.IsPropertyAccessor() == true;

        public bool IsEventAccessor()
            => symbol is IMethodSymbol { MethodKind: MethodKind.EventAdd or MethodKind.EventRaise or MethodKind.EventRemove };

        /// <summary>
        /// If the <paramref name="symbol"/> is a method symbol, returns <see langword="true"/> if the method's return type is "awaitable", but not if it's <see langword="dynamic"/>.
        /// If the <paramref name="symbol"/> is a type symbol, returns <see langword="true"/> if that type is "awaitable".
        /// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter". This GetAwaiter method may be an instance method or an extension method.
        /// </summary>
        public bool IsAwaitableNonDynamic(SemanticModel semanticModel, int position)
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

        public bool HasAttribute([NotNullWhen(true)] INamedTypeSymbol? attributeClass)
        {
            if (symbol is null || attributeClass is null)
                return false;

            return symbol.GetAttributes().Any(static (attribute, attributeClass) => attributeClass.Equals(attribute.AttributeClass), attributeClass);
        }
    }

    extension([NotNullWhen(true)] ISymbol? member)
    {
        /// <summary>
        /// Returns <see langword="true"/> if the signature of this symbol requires the <see
        /// langword="unsafe"/> modifier.  For example a method that takes <c>List&lt;int*[]&gt;</c>
        /// is unsafe, as is <c>int* Goo { get; }</c>.  This will return <see langword="false"/> for
        /// symbols that cannot have the <see langword="unsafe"/> modifier on them.
        /// </summary>
        public bool RequiresUnsafeModifier()
        {
            // TODO(cyrusn): Defer to compiler code to handle this once it can.
            return member?.Accept(new RequiresUnsafeModifierVisitor()) == true;
        }
    }

    extension(IMethodSymbol symbol)
    {
        public bool IsValidGetAwaiter()
        => symbol.Name == WellKnownMemberNames.GetAwaiter &&
        VerifyGetAwaiter(symbol);

        public bool IsValidGetEnumerator()
            => symbol.Name == WellKnownMemberNames.GetEnumeratorMethodName &&
               VerifyGetEnumerator(symbol);

        public bool IsValidGetAsyncEnumerator()
            => symbol.Name == WellKnownMemberNames.GetAsyncEnumeratorMethodName &&
                VerifyGetAsyncEnumerator(symbol);
    }

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
        if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid && x.Parameters is [{ Type.TypeKind: TypeKind.Delegate }]))
            return false;

        // void GetResult() || T GetResult()
        return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
    }

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
}
