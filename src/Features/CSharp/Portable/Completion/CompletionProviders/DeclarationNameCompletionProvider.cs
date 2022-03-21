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
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(DeclarationNameCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(TupleNameCompletionProvider))]
    [Shared]
    internal partial class DeclarationNameCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DeclarationNameCompletionProvider()
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, insertedCharacterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            try
            {
                var position = completionContext.Position;
                var document = completionContext.Document;
                var cancellationToken = completionContext.CancellationToken;
                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);

                if (!completionContext.CompletionOptions.ShowNameSuggestions)
                {
                    return;
                }

                var context = CSharpSyntaxContext.CreateContext(document, semanticModel, position, cancellationToken);
                if (context.IsInNonUserCode)
                {
                    return;
                }

                var nameInfo = await NameDeclarationInfo.GetDeclarationInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<(string name, SymbolKind kind)>.GetInstance(out var result);

                // Suggest names from existing overloads.
                if (nameInfo.PossibleSymbolKinds.Any(k => k.SymbolKind == SymbolKind.Parameter))
                {
                    var (_, partialSemanticModel) = await document.GetPartialSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    if (partialSemanticModel is not null)
                        AddNamesFromExistingOverloads(context, partialSemanticModel, result, cancellationToken);
                }

                var baseNames = GetBaseNames(semanticModel, nameInfo);
                if (baseNames != default)
                {
                    await GetRecommendedNamesAsync(baseNames, nameInfo, context, document, result, cancellationToken).ConfigureAwait(false);
                }

                var recommendedNames = result.ToImmutable();

                if (recommendedNames.IsEmpty)
                    return;

                var sortValue = 0;
                foreach (var (name, kind) in recommendedNames)
                {
                    // We've produced items in the desired order, add a sort text to each item to prevent alphabetization
                    completionContext.AddItem(CreateCompletionItem(name, GetGlyph(kind, nameInfo.DeclaredAccessibility), sortValue.ToString("D8")));
                    sortValue++;
                }

                completionContext.SuggestionModeItem = CommonCompletionItem.Create(
                    CSharpFeaturesResources.Name, displayTextSuffix: "", CompletionItemRules.Default);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
            {
                // nop
            }
        }

        private ImmutableArray<ImmutableArray<string>> GetBaseNames(SemanticModel semanticModel, NameDeclarationInfo nameInfo)
        {
            if (nameInfo.Alias != null)
            {
                return NameGenerator.GetBaseNames(nameInfo.Alias);
            }

            if (!IsValidType(nameInfo.Type))
            {
                return default;
            }

            var (type, plural) = UnwrapType(nameInfo.Type, semanticModel.Compilation, wasPlural: false, seenTypes: new HashSet<ITypeSymbol>());

            var baseNames = NameGenerator.GetBaseNames(type, plural);
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

        private static Glyph GetGlyph(SymbolKind kind, Accessibility? declaredAccessibility)
        {
            var publicIcon = kind switch
            {
                SymbolKind.Field => Glyph.FieldPublic,
                SymbolKind.Local => Glyph.Local,
                SymbolKind.Method => Glyph.MethodPublic,
                SymbolKind.Parameter => Glyph.Parameter,
                SymbolKind.Property => Glyph.PropertyPublic,
                SymbolKind.RangeVariable => Glyph.RangeVariable,
                SymbolKind.TypeParameter => Glyph.TypeParameter,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };

            switch (declaredAccessibility)
            {
                case Accessibility.Private:
                    publicIcon += Glyph.ClassPrivate - Glyph.ClassPublic;
                    break;

                case Accessibility.Protected:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                    publicIcon += Glyph.ClassProtected - Glyph.ClassPublic;
                    break;

                case Accessibility.Internal:
                    publicIcon += Glyph.ClassInternal - Glyph.ClassPublic;
                    break;
            }

            return publicIcon;
        }

        private (ITypeSymbol, bool plural) UnwrapType(ITypeSymbol type, Compilation compilation, bool wasPlural, HashSet<ITypeSymbol> seenTypes)
        {
            // Consider C : Task<C>
            // Visiting the C in Task<C> will stackoverflow
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

        private static async Task GetRecommendedNamesAsync(
            ImmutableArray<ImmutableArray<string>> baseNames,
            NameDeclarationInfo declarationInfo,
            CSharpSyntaxContext context,
            Document document,
            ArrayBuilder<(string name, SymbolKind kind)> result,
            CancellationToken cancellationToken)
        {
            var rules = await document.GetNamingRulesAsync(FallbackNamingRules.CompletionFallbackRules, cancellationToken).ConfigureAwait(false);
            var supplementaryRules = FallbackNamingRules.CompletionSupplementaryRules;
            var semanticFactsService = context.GetLanguageService<ISemanticFactsService>();

            using var _1 = PooledHashSet<string>.GetInstance(out var seenBaseNames);
            using var _2 = PooledHashSet<string>.GetInstance(out var seenUniqueNames);

            foreach (var kind in declarationInfo.PossibleSymbolKinds)
            {
                ProcessRules(rules, firstMatchOnly: true, kind, baseNames, declarationInfo, context, result, semanticFactsService, seenBaseNames, seenUniqueNames, cancellationToken);
                ProcessRules(supplementaryRules, firstMatchOnly: false, kind, baseNames, declarationInfo, context, result, semanticFactsService, seenBaseNames, seenUniqueNames, cancellationToken);
            }

            static void ProcessRules(
                ImmutableArray<NamingRule> rules,
                bool firstMatchOnly,
                SymbolSpecification.SymbolKindOrTypeKind kind,
                ImmutableArray<ImmutableArray<string>> baseNames,
                NameDeclarationInfo declarationInfo,
                CSharpSyntaxContext context,
                ArrayBuilder<(string name, SymbolKind kind)> result,
                ISemanticFactsService semanticFactsService,
                PooledHashSet<string> seenBaseNames,
                PooledHashSet<string> seenUniqueNames,
                CancellationToken cancellationToken)
            {
                // There's no special glyph for local functions.
                // We don't need to differentiate them at this point.
                var symbolKind =
                    kind.SymbolKind.HasValue ? kind.SymbolKind.Value :
                    kind.MethodKind.HasValue ? SymbolKind.Method :
                    throw ExceptionUtilities.Unreachable;

                var modifiers = declarationInfo.Modifiers;
                foreach (var rule in rules)
                {
                    if (rule.SymbolSpecification.AppliesTo(kind, declarationInfo.Modifiers, declarationInfo.DeclaredAccessibility))
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
                                    context.TargetToken.Parent,
                                    containerOpt: null,
                                    baseName: name,
                                    filter: s => IsRelevantSymbolKind(s),
                                    usedNames: Enumerable.Empty<string>(),
                                    cancellationToken: cancellationToken);
                                if (seenUniqueNames.Add(uniqueName.Text))
                                    result.Add((uniqueName.Text, symbolKind));
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

        private static void AddNamesFromExistingOverloads(CSharpSyntaxContext context, SemanticModel semanticModel, ArrayBuilder<(string name, SymbolKind kind)> result, CancellationToken cancellationToken)
        {
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
                        result.Add((overloadParameter.Name, SymbolKind.Parameter));
                    }
                }
            }

            return;

            // Local functions
            static ImmutableArray<IMethodSymbol> GetOverloads(INamedTypeSymbol namedType, BaseMethodDeclarationSyntax baseMethod)
            {
                return baseMethod switch
                {
                    MethodDeclarationSyntax method => namedType.GetMembers(method.Identifier.ValueText).OfType<IMethodSymbol>().ToImmutableArray(),
                    ConstructorDeclarationSyntax constructor => namedType.GetMembers(WellKnownMemberNames.InstanceConstructorName).OfType<IMethodSymbol>().ToImmutableArray(),
                    _ => ImmutableArray<IMethodSymbol>.Empty
                };
            }
        }

        /// <summary>
        /// Check if the symbol is a relevant kind.
        /// Only relevant if symbol could cause a conflict with a local variable.
        /// </summary>
        private static bool IsRelevantSymbolKind(ISymbol symbol)
        {
            return symbol.Kind is SymbolKind.Local or
                SymbolKind.Parameter or
                SymbolKind.RangeVariable;
        }

        private static CompletionItem CreateCompletionItem(string name, Glyph glyph, string sortText)
        {
            return CommonCompletionItem.Create(
                name,
                displayTextSuffix: "",
                CompletionItemRules.Default,
                glyph: glyph,
                sortText: sortText,
                description: CSharpFeaturesResources.Suggested_name.ToSymbolDisplayParts());
        }
    }
}
