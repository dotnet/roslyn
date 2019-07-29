// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class DeclarationNameCompletionProvider : CommonCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, insertedCharacterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            try
            {
                var position = completionContext.Position;
                var document = completionContext.Document;
                var cancellationToken = completionContext.CancellationToken;
                var semanticModel = await document.GetSemanticModelForSpanAsync(new Text.TextSpan(position, 0), cancellationToken).ConfigureAwait(false);

                if (!completionContext.Options.GetOption(CompletionOptions.ShowNameSuggestions, LanguageNames.CSharp))
                {
                    return;
                }

                var context = CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
                if (context.IsInNonUserCode)
                {
                    return;
                }

                var nameInfo = await NameDeclarationInfo.GetDeclarationInfo(document, position, cancellationToken).ConfigureAwait(false);
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

        private bool IsValidType(ITypeSymbol type)
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

        private Glyph GetGlyph(SymbolKind kind, Accessibility? declaredAccessibility)
        {
            Glyph publicIcon;
            switch (kind)
            {
                case SymbolKind.Field:
                    publicIcon = Glyph.FieldPublic;
                    break;
                case SymbolKind.Local:
                    publicIcon = Glyph.Local;
                    break;
                case SymbolKind.Method:
                    publicIcon = Glyph.MethodPublic;
                    break;
                case SymbolKind.Parameter:
                    publicIcon = Glyph.Parameter;
                    break;
                case SymbolKind.Property:
                    publicIcon = Glyph.PropertyPublic;
                    break;
                case SymbolKind.RangeVariable:
                    publicIcon = Glyph.RangeVariable;
                    break;
                case SymbolKind.TypeParameter:
                    publicIcon = Glyph.TypeParameter;
                    break;
                default:
                    throw new ArgumentException();
            }

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

        private async Task<ImmutableArray<(string, SymbolKind)>> GetRecommendedNamesAsync(
            ImmutableArray<ImmutableArray<string>> baseNames,
            NameDeclarationInfo declarationInfo,
            CSharpSyntaxContext context,
            Document document,
            CancellationToken cancellationToken)
        {
            var rules = await document.GetNamingRulesAsync(FallbackNamingRules.CompletionOfferingRules, cancellationToken).ConfigureAwait(false);
            var result = new Dictionary<string, SymbolKind>();
            var semanticFactsService = context.GetLanguageService<ISemanticFactsService>();

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
                            if (name.Length > 1 && !result.ContainsKey(name)) // Don't add multiple items for the same name
                            {
                                var targetToken = context.TargetToken;
                                var uniqueName = semanticFactsService.GenerateUniqueName(
                                    context.SemanticModel,
                                    context.TargetToken.Parent,
                                    containerOpt: null,
                                    baseName: name,
                                    filter: IsRelevantSymbolKind,
                                    usedNames: Enumerable.Empty<string>(),
                                    cancellationToken: cancellationToken);
                                result.Add(uniqueName.Text, symbolKind);
                            }
                        }
                    }
                }
            }

            return result.Select(kvp => (kvp.Key, kvp.Value)).ToImmutableArray();
        }

        /// <summary>
        /// Check if the symbol is a relevant kind.
        /// Only relevant if symbol could cause a conflict with a local variable.
        /// </summary>
        private bool IsRelevantSymbolKind(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Local ||
                symbol.Kind == SymbolKind.Parameter ||
                symbol.Kind == SymbolKind.RangeVariable;
        }

        CompletionItem CreateCompletionItem(string name, Glyph glyph, string sortText)
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
