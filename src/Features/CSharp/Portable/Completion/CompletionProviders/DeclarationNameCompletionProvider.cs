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
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

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
            var position = completionContext.Position;
            var document = completionContext.Document;
            var cancellationToken = completionContext.CancellationToken;
            var semanticModel = await document.GetSemanticModelForSpanAsync(new Text.TextSpan(position, 0), cancellationToken).ConfigureAwait(false);

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
            int sortValue = 0;
            foreach (var (name, kind) in recommendedNames)
            {
                // We've produced items in the desired order, add a sort text to each item to prevent alphabetization
                completionContext.AddItem(CreateCompletionItem(name, GetGlyph(kind, nameInfo.DeclaredAccessibility), sortValue.ToString("D8")));
                sortValue++;
            }

            completionContext.SuggestionModeItem = CommonCompletionItem.Create(CSharpFeaturesResources.Name, CompletionItemRules.Default);
        }

        private ImmutableArray<ImmutableArray<string>> GetBaseNames(SemanticModel semanticModel,  NameDeclarationInfo nameInfo)
        {
            if (nameInfo.Alias != null)
            {
                return NameGenerator.GetBaseNames(nameInfo.Alias);
            }

            if (!IsValidType(nameInfo.Type))
            {
                return default;
            }

            var (type, plural) = UnwrapType(nameInfo.Type, semanticModel.Compilation, wasPlural: false);

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

        private Glyph GetGlyph(SymbolKind kind, Accessibility declaredAccessibility)
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

        private (ITypeSymbol, bool plural) UnwrapType(ITypeSymbol type, Compilation compilation, bool wasPlural)
        {
            if (type is INamedTypeSymbol namedType && namedType.OriginalDefinition != null)
            {
                var originalDefinition = namedType.OriginalDefinition;
                
                var ienumerableOfT = namedType.GetAllInterfacesIncludingThis().FirstOrDefault(
                    t => t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

                if (ienumerableOfT != null)
                {
                    return UnwrapType(ienumerableOfT.TypeArguments[0], compilation, wasPlural: true);
                }

                var taskOfTType = compilation.TaskOfTType();
                var valueTaskType = compilation.ValueTaskOfTType();

                if (originalDefinition == taskOfTType
                    ||originalDefinition == valueTaskType
                    || originalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return UnwrapType(namedType.TypeArguments[0], compilation, wasPlural: wasPlural);
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
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var namingStyleOptions = options.GetOption(SimplificationOptions.NamingPreferences);
            var rules = namingStyleOptions.CreateRules().NamingRules.Concat(s_BuiltInRules);
            var result = new Dictionary<string, SymbolKind>();
            foreach (var symbolKind in declarationInfo.PossibleSymbolKinds)
            {
                var kind = new SymbolKindOrTypeKind(symbolKind);
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
                                result.Add(name, symbolKind);
                            }
                        }
                    }
                }
            }

            return result.Select(kvp => (kvp.Key, kvp.Value)).ToImmutableArray();
        }

        CompletionItem CreateCompletionItem(string name, Glyph glyph, string sortText)
        {
            return CommonCompletionItem.Create(
                name, 
                CompletionItemRules.Default, 
                glyph: glyph, 
                sortText: sortText, 
                description: CSharpFeaturesResources.Suggested_name.ToSymbolDisplayParts());
        }
    }
}
