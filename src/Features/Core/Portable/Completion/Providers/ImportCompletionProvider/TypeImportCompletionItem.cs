// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class TypeImportCompletionItem
    {
        private const string SortTextFormat = "~{0} {1}";
        private const string GenericTypeNameManglingString = "`";
        private static readonly string[] s_aritySuffixesOneToNine = { "`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9" };

        private const string TypeAritySuffixName = nameof(TypeAritySuffixName);
        private const string AttributeFullName = nameof(AttributeFullName);

        public static CompletionItem Create(INamedTypeSymbol typeSymbol, string containingNamespace, string genericTypeSuffix)
        {
            PooledDictionary<string, string> propertyBuilder = null;

            if (typeSymbol.Arity > 0)
            {
                propertyBuilder = PooledDictionary<string, string>.GetInstance();
                propertyBuilder.Add(TypeAritySuffixName, GetAritySuffix(typeSymbol.Arity));
            }

            // Add tildes (ASCII: 126) to name and namespace as sort text:
            // 1. '~' before type name makes import items show after in-scope items
            // 2. ' ' before namespace makes types with identical type name but from different namespace all show up in the list,
            //    it also makes sure type with shorter name shows first, e.g. 'SomeType` before 'SomeTypeWithLongerName'.  
            var sortTextBuilder = PooledStringBuilder.GetInstance();
            sortTextBuilder.Builder.AppendFormat(SortTextFormat, typeSymbol.Name, containingNamespace);
            
            var item = CompletionItem.Create(
                 displayText: typeSymbol.Name,
                 filterText: typeSymbol.Name,
                 sortText: sortTextBuilder.ToStringAndFree(),
                 properties: propertyBuilder?.ToImmutableDictionaryAndFree(),
                 tags: GlyphTags.GetTags(typeSymbol.GetGlyph()),
                 rules: CompletionItemRules.Default,
                 displayTextPrefix: null,
                 displayTextSuffix: typeSymbol.Arity == 0 ? string.Empty : genericTypeSuffix,
                 inlineDescription: containingNamespace);

            item.Flags = CompletionItemFlags.CachedAndExpanded;
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
                 filterText: attributeNameWithoutSuffix,
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
            var metadataName = GetMetadataName(item);
            if (!string.IsNullOrEmpty(metadataName))
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var symbol = compilation.GetTypeByMetadataName(metadataName);
                if (symbol != null)
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    // We choose not to display the number of "type overloads" for simplicity. 
                    // Otherwise, we need additonal logic to track internal and public visible
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
            }

            return null;
        }

        private static string GetAritySuffix(int arity)
        {
            Debug.Assert(arity > 0);
            return (arity <= s_aritySuffixesOneToNine.Length)
                ? s_aritySuffixesOneToNine[arity - 1]
                : string.Concat(GenericTypeNameManglingString, arity.ToString(CultureInfo.InvariantCulture));
        }

        private static string GetFullyQualifiedName(string namespaceName, string typeName)
            => namespaceName.Length == 0 ? typeName : namespaceName + "." + typeName;

        private static string GetMetadataName(CompletionItem item)
        {
            var containingNamespace = GetContainingNamespace(item);
            var typeName = item.Properties.TryGetValue(AttributeFullName, out var attributeFullName) ? attributeFullName : item.DisplayText;
            var fullyQualifiedName = GetFullyQualifiedName(containingNamespace, typeName);
            if (item.Properties.TryGetValue(TypeAritySuffixName, out var aritySuffix))
            {
                return fullyQualifiedName + aritySuffix;
            }

            return fullyQualifiedName;
        }
    }
}
