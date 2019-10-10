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
        private const string SymbolName = nameof(SymbolName);

        public static CompletionItem Create(INamedTypeSymbol typeSymbol, string containingNamespace, string genericTypeSuffix)
        {
            return Create(typeSymbol, typeSymbol.Arity, containingNamespace, genericTypeSuffix, CompletionItemFlags.CachedAndExpanded, encodeSymbol: false);
        }

        public static CompletionItem Create(IMethodSymbol methodSymbol, string containingNamespace, string genericTypeSuffix)
        {
            Debug.Assert(methodSymbol.IsExtensionMethod);
            return Create(methodSymbol, methodSymbol.Arity, containingNamespace, genericTypeSuffix, CompletionItemFlags.Expanded, encodeSymbol: true);
        }

        private static CompletionItem Create(ISymbol symbol, int arity, string containingNamespace, string genericTypeSuffix, CompletionItemFlags flags, bool encodeSymbol)
        {
            ImmutableDictionary<string, string>? properties = null;

            if (encodeSymbol || arity > 0)
            {
                var builder = PooledDictionary<string, string>.GetInstance();

                if (encodeSymbol)
                {
                    builder.Add(SymbolName, SymbolCompletionItem.EncodeSymbol(symbol));
                }
                else
                {
                    builder.Add(TypeAritySuffixName, AbstractDeclaredSymbolInfoFactoryService.GetMetadataAritySuffix(arity));
                }

                properties = builder.ToImmutableDictionaryAndFree();
            }

            // Add tildes (ASCII: 126) to name and namespace as sort text:
            // 1. '~' before type name makes import items show after in-scope items
            // 2. ' ' before namespace makes types with identical type name but from different namespace all show up in the list,
            //    it also makes sure type with shorter name shows first, e.g. 'SomeType` before 'SomeTypeWithLongerName'.  
            var sortTextBuilder = PooledStringBuilder.GetInstance();
            sortTextBuilder.Builder.AppendFormat(SortTextFormat, symbol.Name, containingNamespace);

            var item = CompletionItem.Create(
                 displayText: symbol.Name,
                 sortText: sortTextBuilder.ToStringAndFree(),
                 properties: properties,
                 tags: GlyphTags.GetTags(symbol.GetGlyph()),
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
        {
            return item.WithDisplayTextSuffix(genericTypeSuffix);
        }

        public static string GetContainingNamespace(CompletionItem item)
            => item.InlineDescription;

        public static async Task<CompletionDescription> GetCompletionDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var compilation = (await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
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
            if (item.Properties.TryGetValue(SymbolName, out var symbolId))
            {
                return SymbolCompletionItem.DecodeSymbol(symbolId, compilation);
            }

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
