// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName;

[ExportDeclarationNameRecommender(nameof(DeclarationNameRecommender)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class DeclarationNameRecommender() : IDeclarationNameRecommender
{
    public async Task<ImmutableArray<(string name, Glyph glyph)>> ProvideRecommendedNamesAsync(
        CompletionContext completionContext,
        Document document,
        CSharpSyntaxContext context,
        NameDeclarationInfo nameInfo,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<(string, Glyph)>.GetInstance(out var result);

        // Suggest names from existing overloads.
        if (nameInfo.PossibleSymbolKinds.Any(static k => k.SymbolKind == SymbolKind.Parameter))
            AddNamesFromExistingOverloads(context, nameInfo, result, cancellationToken);

        var names = GetBaseNames(context.SemanticModel, nameInfo).NullToEmpty();

        // If we have a direct symbol this binds to, offer its name as a potential name here.
        if (nameInfo.Symbol != null)
            names = names.Insert(0, [nameInfo.Symbol.Name]);

        if (!names.IsDefaultOrEmpty)
        {
            var namingStyleOptions = await document.GetNamingStylePreferencesAsync(cancellationToken).ConfigureAwait(false);
            GetRecommendedNames(names, nameInfo, context, result, namingStyleOptions, cancellationToken);
        }

        return result.ToImmutableAndClear();
    }

    private ImmutableArray<ImmutableArray<string>> GetBaseNames(SemanticModel semanticModel, NameDeclarationInfo nameInfo)
    {
        if (nameInfo.Alias != null)
            return NameGenerator.GetBaseNames(nameInfo.Alias);

        if (!IsValidType(nameInfo.Type))
            return default;

        var compilation = semanticModel.Compilation;
        var originalType = nameInfo.Type;

        var (type, plural) = UnwrapType(originalType, compilation, wasPlural: false, seenTypes: []);
        var baseNames = NameGenerator.GetBaseNames(type, plural);

        // Check if the original type is a Func<..., T> and add special suggestions
        if (originalType is INamedTypeSymbol { Name: "Func", ContainingNamespace.Name: "System", TypeArguments: [.., var returnType] })
        {
            using var result = TemporaryArray<ImmutableArray<string>>.Empty;

            // Add standalone suggestions
            result.Add(["Factory"]);
            result.Add(["Selector"]);

            // Get names based on the original Func type itself (e.g., "func" for Func<T>)
            result.AddRange(baseNames);

            // Also unwrap the return type and get names from it
            var (unwrappedReturnType, returnTypePlural) = UnwrapType(returnType, compilation, wasPlural: false, seenTypes: []);
            var returnTypeBaseNames = NameGenerator.GetBaseNames(unwrappedReturnType, returnTypePlural);

            // Add return type base names with "Factory" suffix
            foreach (var baseName in returnTypeBaseNames)
                result.Add([.. baseName, "Factory"]);

            return result.ToImmutableAndClear();
        }

        return baseNames;
    }

    private static bool IsValidType([NotNullWhen(true)] ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.IsErrorType() && (type.Name == "var" || type.Name == string.Empty))
        {
            return false;
        }

        if (type.SpecialType == SpecialType.System_Void)
        {
            return false;
        }

        return !type.IsSpecialType();
    }

    private (ITypeSymbol, bool plural) UnwrapType(ITypeSymbol type, Compilation compilation, bool wasPlural, HashSet<ITypeSymbol> seenTypes)
    {
        // Consider C : Task<C>
        // Visiting the C in Task<C> will stack overflow
        if (seenTypes.Contains(type))
        {
            return (type, wasPlural);
        }

        // The main purpose of this is to prevent converting "string" to "chars", but it also simplifies logic for other basic types (int, double, object etc.)
        if (type.IsSpecialType())
        {
            return (type, wasPlural);
        }

        seenTypes.AddRange(type.GetBaseTypesAndThis());

        if (type is IArrayTypeSymbol arrayType)
        {
            return UnwrapType(arrayType.ElementType, compilation, wasPlural: true, seenTypes: seenTypes);
        }

        if (type is IErrorTypeSymbol { TypeArguments: [var typeArgument] } &&
            LooksLikeWellKnownCollectionType(compilation, type.Name))
        {
            return UnwrapType(typeArgument, compilation, wasPlural: true, seenTypes);
        }

        if (type is INamedTypeSymbol namedType && namedType.OriginalDefinition != null)
        {
            // if namedType contains a valid GetEnumerator method, we want collectionType to be the type of
            // the "Current" property of this enumerator. For example:
            // if namedType is a Span<Person>, collectionType should be Person.
            var collectionType = namedType.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.IsValidGetEnumerator() || m.IsValidGetAsyncEnumerator())
                ?.ReturnType?.GetMembers(WellKnownMemberNames.CurrentPropertyName)
                .OfType<IPropertySymbol>().FirstOrDefault(p => p.GetMethod != null)?.Type;

            // This can happen for an un-implemented IEnumerable or IAsyncEnumerable.
            collectionType ??= namedType.AllInterfaces.FirstOrDefault(
                    t => t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
                         Equals(t.OriginalDefinition, compilation.IAsyncEnumerableOfTType()))?.TypeArguments[0];

            if (collectionType is not null)
            {
                // Consider: Container : IEnumerable<Container>
                // Container |
                // We don't want to suggest the plural version of a type that can be used singularly
                if (seenTypes.Contains(collectionType))
                {
                    return (type, wasPlural);
                }

                return UnwrapType(collectionType, compilation, wasPlural: true, seenTypes: seenTypes);
            }

            var originalDefinition = namedType.OriginalDefinition;
            var taskOfTType = compilation.TaskOfTType();
            var valueTaskType = compilation.ValueTaskOfTType();
            var lazyOfTType = compilation.LazyOfTType();

            if (Equals(originalDefinition, taskOfTType) ||
                Equals(originalDefinition, valueTaskType) ||
                Equals(originalDefinition, lazyOfTType) ||
                originalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return UnwrapType(namedType.TypeArguments[0], compilation, wasPlural: wasPlural, seenTypes: seenTypes);
            }
        }

        return (type, wasPlural);
    }

    private bool LooksLikeWellKnownCollectionType(Compilation compilation, string name)
    {
        // see if the user has something like `IEnumerable<Customer>` (where IEnumerable doesn't bind).  Weak
        // heuristic.  If there's a matching type under System.Collections with that name, then assume it's a
        // collection and attempt to create a name from the type arg.
        var system = compilation.GlobalNamespace.GetMembers(nameof(System)).OfType<INamespaceSymbol>().FirstOrDefault();
        var systemCollections = system?.GetMembers(nameof(System.Collections)).OfType<INamespaceSymbol>().FirstOrDefault();

        // just check System.Collections, and it's immediate namespace children.  This covers all the common cases
        // like "Concurrent/Generic/Immutable/Specialized", and prevents having to worry about huge trees to walk.
        if (systemCollections is not null)
        {
            if (Check(systemCollections, name))
                return true;

            foreach (var childNamespace in systemCollections.GetNamespaceMembers())
            {
                if (Check(childNamespace, name))
                    return true;
            }
        }

        return false;

        static bool Check(INamespaceSymbol? namespaceSymbol, string name)
            => namespaceSymbol != null && namespaceSymbol.GetTypeMembers(name).Any(static t => t.DeclaredAccessibility == Accessibility.Public);
    }

    private static void GetRecommendedNames(
        ImmutableArray<ImmutableArray<string>> baseNames,
        NameDeclarationInfo declarationInfo,
        CSharpSyntaxContext context,
        ArrayBuilder<(string, Glyph)> result,
        NamingStylePreferences namingStyleOptions,
        CancellationToken cancellationToken)
    {
        var rules = namingStyleOptions.Rules.NamingRules.AddRange(FallbackNamingRules.CompletionFallbackRules);

        var supplementaryRules = FallbackNamingRules.CompletionSupplementaryRules;
        var semanticFactsService = context.GetRequiredLanguageService<ISemanticFactsService>();

        using var _1 = PooledHashSet<string>.GetInstance(out var seenBaseNames);
        using var _2 = PooledHashSet<string>.GetInstance(out var seenUniqueNames);

        foreach (var kind in declarationInfo.PossibleSymbolKinds)
        {
            ProcessRules(rules, firstMatchOnly: true, kind);
            ProcessRules(supplementaryRules, firstMatchOnly: false, kind);
        }

        void ProcessRules(
            ImmutableArray<NamingRule> rules,
            bool firstMatchOnly,
            SymbolSpecification.SymbolKindOrTypeKind kind)
        {
            var modifiers = declarationInfo.Modifiers;
            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(kind, declarationInfo.Modifiers.Modifiers, declarationInfo.DeclaredAccessibility))
                {
                    foreach (var baseName in baseNames)
                    {
                        var name = rule.NamingStyle.CreateName(baseName).EscapeIdentifier(context.IsInQuery);

                        // Don't add multiple items for the same name and only add valid identifiers
                        if (name.Length > 1 &&
                            name != CodeAnalysis.Shared.Extensions.ITypeSymbolExtensions.DefaultParameterName &&
                            CSharpSyntaxFacts.Instance.IsValidIdentifier(name) &&
                            seenBaseNames.Add(name))
                        {
                            var uniqueName = semanticFactsService.GenerateUniqueName(
                                context.SemanticModel,
                                context.TargetToken.GetRequiredParent(),
                                container: null,
                                baseName: name,
                                filter: IsRelevantSymbolKind,
                                usedNames: [],
                                cancellationToken);

                            if (seenUniqueNames.Add(uniqueName.Text))
                            {
                                result.Add((uniqueName.Text,
                                    NameDeclarationInfo.GetGlyph(NameDeclarationInfo.GetSymbolKind(kind), declarationInfo.DeclaredAccessibility)));
                            }
                        }
                    }

                    if (firstMatchOnly)
                    {
                        // Only consider the first matching specification for each potential symbol or type kind.
                        // https://github.com/dotnet/roslyn/issues/36248
                        break;
                    }
                }
            }
        }
    }

    private static void AddNamesFromExistingOverloads(
        CSharpSyntaxContext context, NameDeclarationInfo declarationInfo, ArrayBuilder<(string, Glyph)> result, CancellationToken cancellationToken)
    {
        var semanticModel = context.SemanticModel;
        var namedType = semanticModel.GetEnclosingNamedType(context.Position, cancellationToken);
        if (namedType is null)
            return;

        var parameterSyntax = context.LeftToken.GetAncestor(n => n.IsKind(SyntaxKind.Parameter)) as ParameterSyntax;
        if (parameterSyntax is not { Type: { } parameterType, Parent.Parent: BaseMethodDeclarationSyntax baseMethod })
            return;

        var methodParameterType = semanticModel.GetTypeInfo(parameterType, cancellationToken).Type;
        if (methodParameterType is null)
            return;

        var overloads = GetOverloads(namedType, baseMethod);
        if (overloads.IsEmpty)
            return;

        var currentParameterNames = baseMethod.ParameterList.Parameters.Select(p => p.Identifier.ValueText).ToImmutableHashSet();

        foreach (var overload in overloads)
        {
            foreach (var overloadParameter in overload.Parameters)
            {
                if (!currentParameterNames.Contains(overloadParameter.Name) &&
                    methodParameterType.Equals(overloadParameter.Type, SymbolEqualityComparer.Default))
                {
                    result.Add((overloadParameter.Name, NameDeclarationInfo.GetGlyph(SymbolKind.Parameter, declarationInfo.DeclaredAccessibility)));
                }
            }
        }

        return;

        // Local functions
        static ImmutableArray<IMethodSymbol> GetOverloads(INamedTypeSymbol namedType, BaseMethodDeclarationSyntax baseMethod)
        {
            return baseMethod switch
            {
                MethodDeclarationSyntax method => [.. namedType.GetMembers(method.Identifier.ValueText).OfType<IMethodSymbol>()],
                ConstructorDeclarationSyntax constructor => [.. namedType.GetMembers(WellKnownMemberNames.InstanceConstructorName).OfType<IMethodSymbol>()],
                _ => []
            };
        }
    }

    /// <summary>
    /// Check if the symbol is a relevant kind.
    /// Only relevant if symbol could cause a conflict with a local variable.
    /// </summary>
    private static bool IsRelevantSymbolKind(ISymbol symbol)
        => symbol.Kind is SymbolKind.Local or SymbolKind.Parameter or SymbolKind.RangeVariable;
}
