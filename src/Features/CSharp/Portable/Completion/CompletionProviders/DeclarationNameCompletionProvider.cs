// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
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

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, insertedCharacterPosition, options);

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            try
            {
                var position = completionContext.Position;
                var document = completionContext.Document;
                var cancellationToken = completionContext.CancellationToken;
                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);

                if (!completionContext.Options.GetOption(CompletionOptions.ShowNameSuggestions, LanguageNames.CSharp))
                {
                    return;
                }

                var context = CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
                if (context.IsInNonUserCode)
                {
                    return;
                }

                var nameInfo = await NameDeclarationInfo.GetDeclarationInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                var baseNames = GetBaseNames(semanticModel, nameInfo);
                if (baseNames == default)
                {
                    return;
                }

                var recommendedNames = await GetRecommendedNamesAsync(baseNames, nameInfo, context, document, cancellationToken).ConfigureAwait(false);
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
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
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

        private static bool IsValidType(ITypeSymbol type)
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
                _ => throw new ArgumentException(),
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

            seenTypes.AddRange(type.GetBaseTypesAndThis());

            if (type is IArrayTypeSymbol arrayType)
            {
                return UnwrapType(arrayType.ElementType, compilation, wasPlural: true, seenTypes: seenTypes);
            }

            if (type is INamedTypeSymbol namedType && namedType.OriginalDefinition != null)
            {
                var originalDefinition = namedType.OriginalDefinition;

                var ienumerableOfT = namedType.GetAllInterfacesIncludingThis().FirstOrDefault(
                    t => t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

                if (ienumerableOfT != null)
                {
                    // Consider: Container : IEnumerable<Container>
                    // Container |
                    // We don't want to suggest the plural version of a type that can be used singularly
                    if (seenTypes.Contains(ienumerableOfT.TypeArguments[0]))
                    {
                        return (type, wasPlural);
                    }

                    return UnwrapType(ienumerableOfT.TypeArguments[0], compilation, wasPlural: true, seenTypes: seenTypes);
                }

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

        private static async Task<ImmutableArray<(string name, SymbolKind kind)>> GetRecommendedNamesAsync(
            ImmutableArray<ImmutableArray<string>> baseNames,
            NameDeclarationInfo declarationInfo,
            CSharpSyntaxContext context,
            Document document,
            CancellationToken cancellationToken)
        {
            var rules = await document.GetNamingRulesAsync(FallbackNamingRules.CompletionOfferingRules, cancellationToken).ConfigureAwait(false);
            var semanticFactsService = context.GetLanguageService<ISemanticFactsService>();

            using var _1 = PooledHashSet<string>.GetInstance(out var seenBaseNames);
            using var _2 = PooledHashSet<string>.GetInstance(out var seenUniqueNames);
            using var _3 = ArrayBuilder<(string name, SymbolKind kind)>.GetInstance(out var result);

            foreach (var kind in declarationInfo.PossibleSymbolKinds)
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
                    }
                }
            }

            return result.ToImmutable();
        }

        /// <summary>
        /// Check if the symbol is a relevant kind.
        /// Only relevant if symbol could cause a conflict with a local variable.
        /// </summary>
        private static bool IsRelevantSymbolKind(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Local ||
                symbol.Kind == SymbolKind.Parameter ||
                symbol.Kind == SymbolKind.RangeVariable;
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
