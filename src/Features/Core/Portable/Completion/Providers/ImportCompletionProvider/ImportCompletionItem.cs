// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class ImportCompletionItem
    {
        private const string SortTextFormat = "~{0} {1}";

        private const string TypeAritySuffixName = nameof(TypeAritySuffixName);
        private const string AttributeFullName = nameof(AttributeFullName);
        private const string SymbolKeyData = nameof(SymbolKeyData);

        public static CompletionItem Create(INamedTypeSymbol typeSymbol, string containingNamespace, string genericTypeSuffix)
            => Create(typeSymbol.Name, typeSymbol.Arity, containingNamespace, typeSymbol.GetGlyph(), genericTypeSuffix, CompletionItemFlags.CachedAndExpanded, symbolKeyData: null);

        public static CompletionItem Create(string name, int arity, string containingNamespace, Glyph glyph, string genericTypeSuffix, CompletionItemFlags flags, string? symbolKeyData)
        {
            ImmutableDictionary<string, string>? properties = null;

            if (symbolKeyData != null || arity > 0)
            {
                var builder = PooledDictionary<string, string>.GetInstance();

                if (symbolKeyData != null)
                {
                    builder.Add(SymbolKeyData, symbolKeyData);
                }
                else
                {
                    // We don't need arity to recover symbol if we already have SymbolKeyData or it's 0.
                    // (but it still needed below to decide whether to show generic suffix)
                    builder.Add(TypeAritySuffixName, AbstractDeclaredSymbolInfoFactoryService.GetMetadataAritySuffix(arity));
                }

                properties = builder.ToImmutableDictionaryAndFree();
            }

            // Add tildes (ASCII: 126) to name and namespace as sort text:
            // 1. '~' before type name makes import items show after in-scope items
            // 2. ' ' before namespace makes types with identical type name but from different namespace all show up in the list,
            //    it also makes sure type with shorter name shows first, e.g. 'SomeType` before 'SomeTypeWithLongerName'.  
            var sortTextBuilder = PooledStringBuilder.GetInstance();
            sortTextBuilder.Builder.AppendFormat(SortTextFormat, name, containingNamespace);

            var item = CompletionItem.Create(
                 displayText: name,
                 sortText: sortTextBuilder.ToStringAndFree(),
                 properties: properties,
                 tags: GlyphTags.GetTags(glyph),
                 rules: CompletionItemRules.Default,
                 displayTextPrefix: null,
                 displayTextSuffix: arity == 0 ? string.Empty : genericTypeSuffix,
                 inlineDescription: containingNamespace);

            item.Flags = flags;
            return item;
        }

        public static CompletionItem CreateAttributeItemWithoutSuffix(CompletionItem attributeItem, string attributeNameWithoutSuffix)
        {
            Debug.Assert(!attributeItem.Properties.ContainsKey(AttributeFullName));

            // Remember the full type name so we can get the symbol when description is displayed.
            var newProperties = attributeItem.Properties.Add(AttributeFullName, attributeItem.DisplayText);

            var sortTextBuilder = PooledStringBuilder.GetInstance();
            sortTextBuilder.Builder.AppendFormat(SortTextFormat, attributeNameWithoutSuffix, attributeItem.InlineDescription);

            return CompletionItem.Create(
                 displayText: attributeNameWithoutSuffix,
                 sortText: sortTextBuilder.ToStringAndFree(),
                 properties: newProperties,
                 tags: attributeItem.Tags,
                 rules: attributeItem.Rules,
                 displayTextPrefix: attributeItem.DisplayTextPrefix,
                 displayTextSuffix: attributeItem.DisplayTextSuffix,
                 inlineDescription: attributeItem.InlineDescription);
        }

        public static CompletionItem CreateItemWithGenericDisplaySuffix(CompletionItem item, string genericTypeSuffix)
            => item.WithDisplayTextSuffix(genericTypeSuffix);

        public static string GetContainingNamespace(CompletionItem item)
            => item.InlineDescription;

        public static async Task<CompletionDescription> GetCompletionDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var compilation = (await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false));
            var symbol = GetSymbol(item, compilation);

            if (symbol != null)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // We choose not to display the number of "type overloads" for simplicity. 
                // Otherwise, we need additional logic to track internal and public visible
                // types separately, and cache both completion items.
                return await CommonCompletionUtilities.CreateDescriptionAsync(
                    document.Project.Solution.Workspace,
                    semanticModel,
                    position: 0,
                    symbol,
                    overloadCount: 0,
                    supportedPlatforms: null,
                    cancellationToken).ConfigureAwait(false);
            }

            return CompletionDescription.Empty;
        }

        private static string GetFullyQualifiedName(string namespaceName, string typeName)
            => namespaceName.Length == 0 ? typeName : namespaceName + "." + typeName;

        private static ISymbol? GetSymbol(CompletionItem item, Compilation compilation)
        {
            // If we have SymbolKey data (i.e. this is an extension method item), use it to recover symbol
            if (item.Properties.TryGetValue(SymbolKeyData, out var symbolId))
            {
                return SymbolKey.ResolveString(symbolId, compilation).GetAnySymbol();
            }

            // Otherwise, this is a type item, so we don't have SymbolKey data. But we should still have all
            // the data to construct its full metadata name
            var containingNamespace = GetContainingNamespace(item);
            var typeName = item.Properties.TryGetValue(AttributeFullName, out var attributeFullName) ? attributeFullName : item.DisplayText;
            var fullyQualifiedName = GetFullyQualifiedName(containingNamespace, typeName);
            if (item.Properties.TryGetValue(TypeAritySuffixName, out var aritySuffix))
            {
                return compilation.GetTypeByMetadataName(fullyQualifiedName + aritySuffix);
            }

            return compilation.GetTypeByMetadataName(fullyQualifiedName);
        }
    }
}
