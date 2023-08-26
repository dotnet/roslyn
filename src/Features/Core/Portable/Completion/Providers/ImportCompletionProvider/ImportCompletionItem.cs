// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class ImportCompletionItem
    {
        // Note the additional space as prefix to the System namespace,
        // to make sure items from System.* get sorted ahead.
        private const string OtherNamespaceSortTextFormat = "~{0} {1}";
        private const string SystemNamespaceSortTextFormat = "~{0}  {1}";

        private const string TypeAritySuffixName = nameof(TypeAritySuffixName);
        private const string AttributeFullName = nameof(AttributeFullName);
        private const string MethodKey = nameof(MethodKey);
        private const string ReceiverKey = nameof(ReceiverKey);
        private const string OverloadCountKey = nameof(OverloadCountKey);
        private const string AlwaysFullyQualifyKey = nameof(AlwaysFullyQualifyKey);

        public static CompletionItem Create(
            string name,
            int arity,
            string containingNamespace,
            Glyph glyph,
            string genericTypeSuffix,
            CompletionItemFlags flags,
            (string methodSymbolKey, string receiverTypeSymbolKey, int overloadCount)? extensionMethodData,
            bool includedInTargetTypeCompletion = false)
        {
            ImmutableDictionary<string, string>? properties = null;

            if (extensionMethodData != null || arity > 0)
            {
                var builder = PooledDictionary<string, string>.GetInstance();

                if (extensionMethodData.HasValue)
                {
                    builder.Add(MethodKey, extensionMethodData.Value.methodSymbolKey);
                    builder.Add(ReceiverKey, extensionMethodData.Value.receiverTypeSymbolKey);

                    if (extensionMethodData.Value.overloadCount > 0)
                    {
                        builder.Add(OverloadCountKey, extensionMethodData.Value.overloadCount.ToString());
                    }
                }
                else
                {
                    // We don't need arity to recover symbol if we already have SymbolKeyData or it's 0.
                    // (but it still needed below to decide whether to show generic suffix)
                    builder.Add(TypeAritySuffixName, ArityUtilities.GetMetadataAritySuffix(arity));
                }

                properties = builder.ToImmutableDictionaryAndFree();
            }

            // Use "<display name> <namespace>" as sort text. The space before namespace makes items with identical display name
            // but from different namespace all show up in the list, it also makes sure item with shorter name shows first, 
            // e.g. 'SomeType` before 'SomeTypeWithLongerName'. 
            var sortTextBuilder = PooledStringBuilder.GetInstance();
            sortTextBuilder.Builder.AppendFormat(GetSortTextFormatString(containingNamespace), name, containingNamespace);

            var item = CompletionItem.Create(
                 displayText: name,
                 sortText: sortTextBuilder.ToStringAndFree(),
                 properties: properties,
                 tags: GlyphTags.GetTags(glyph),
                 rules: CompletionItemRules.Default,
                 displayTextPrefix: null,
                 displayTextSuffix: arity == 0 ? string.Empty : genericTypeSuffix,
                 inlineDescription: containingNamespace,
                 isComplexTextEdit: true);

            if (includedInTargetTypeCompletion)
            {
                item = item.AddTag(WellKnownTags.TargetTypeMatch);
            }

            item.Flags = flags;
            return item;
        }

        public static CompletionItem CreateAttributeItemWithoutSuffix(CompletionItem attributeItem, string attributeNameWithoutSuffix, CompletionItemFlags flags)
        {
            Debug.Assert(!attributeItem.Properties.ContainsKey(AttributeFullName));

            // Remember the full type name so we can get the symbol when description is displayed.
            var newProperties = attributeItem.Properties.Add(AttributeFullName, attributeItem.DisplayText);

            var sortTextBuilder = PooledStringBuilder.GetInstance();
            sortTextBuilder.Builder.AppendFormat(GetSortTextFormatString(attributeItem.InlineDescription), attributeNameWithoutSuffix, attributeItem.InlineDescription);

            var item = CompletionItem.Create(
                 displayText: attributeNameWithoutSuffix,
                 sortText: sortTextBuilder.ToStringAndFree(),
                 properties: newProperties,
                 tags: attributeItem.Tags,
                 rules: attributeItem.Rules,
                 displayTextPrefix: attributeItem.DisplayTextPrefix,
                 displayTextSuffix: attributeItem.DisplayTextSuffix,
                 inlineDescription: attributeItem.InlineDescription,
                 isComplexTextEdit: true);

            item.Flags = flags;
            return item;
        }

        private static string GetSortTextFormatString(string containingNamespace)
        {
            if (containingNamespace == "System" || containingNamespace.StartsWith("System."))
                return SystemNamespaceSortTextFormat;

            return OtherNamespaceSortTextFormat;
        }

        public static CompletionItem CreateItemWithGenericDisplaySuffix(CompletionItem item, string genericTypeSuffix)
            => item.WithDisplayTextSuffix(genericTypeSuffix);

        public static string GetContainingNamespace(CompletionItem item)
            => item.InlineDescription;

        public static async Task<CompletionDescription> GetCompletionDescriptionAsync(Document document, CompletionItem item, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var (symbol, overloadCount) = GetSymbolAndOverloadCount(item, compilation);

            if (symbol != null)
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                return await CommonCompletionUtilities.CreateDescriptionAsync(
                    document.Project.Solution.Services,
                    semanticModel,
                    position: 0,
                    symbol,
                    overloadCount,
                    options,
                    supportedPlatforms: null,
                    cancellationToken).ConfigureAwait(false);
            }

            return CompletionDescription.Empty;
        }

        public static string GetTypeName(CompletionItem item)
        {
            var typeName = item.Properties.TryGetValue(AttributeFullName, out var attributeFullName)
                ? attributeFullName
                : item.DisplayText;

            if (item.Properties.TryGetValue(TypeAritySuffixName, out var aritySuffix))
            {
                return typeName + aritySuffix;
            }

            return typeName;
        }

        private static string GetFullyQualifiedName(string namespaceName, string typeName)
            => namespaceName.Length == 0 ? typeName : namespaceName + "." + typeName;

        private static (ISymbol? symbol, int overloadCount) GetSymbolAndOverloadCount(CompletionItem item, Compilation compilation)
        {
            // If we have SymbolKey data (i.e. this is an extension method item), use it to recover symbol
            if (item.Properties.TryGetValue(MethodKey, out var methodSymbolKey))
            {
                var methodSymbol = SymbolKey.ResolveString(methodSymbolKey, compilation).GetAnySymbol() as IMethodSymbol;

                if (methodSymbol != null)
                {
                    var overloadCount = item.Properties.TryGetValue(OverloadCountKey, out var overloadCountString) && int.TryParse(overloadCountString, out var count) ? count : 0;

                    // Get reduced extension method symbol for the given receiver type.
                    if (item.Properties.TryGetValue(ReceiverKey, out var receiverTypeKey))
                    {
                        if (SymbolKey.ResolveString(receiverTypeKey, compilation).GetAnySymbol() is ITypeSymbol receiverTypeSymbol)
                        {
                            return (methodSymbol.ReduceExtensionMethod(receiverTypeSymbol) ?? methodSymbol, overloadCount);
                        }
                    }

                    return (methodSymbol, overloadCount);
                }

                return default;
            }

            // Otherwise, this is a type item, so we don't have SymbolKey data. But we should still have all 
            // the data to construct its full metadata name
            var containingNamespace = GetContainingNamespace(item);
            var typeName = item.Properties.TryGetValue(AttributeFullName, out var attributeFullName) ? attributeFullName : item.DisplayText;
            var fullyQualifiedName = GetFullyQualifiedName(containingNamespace, typeName);

            // We choose not to display the number of "type overloads" for simplicity.
            // Otherwise, we need additional logic to track internal and public visible
            // types separately, and cache both completion items.
            if (item.Properties.TryGetValue(TypeAritySuffixName, out var aritySuffix))
            {
                return (compilation.GetTypeByMetadataName(fullyQualifiedName + aritySuffix), 0);
            }

            return (compilation.GetTypeByMetadataName(fullyQualifiedName), 0);
        }

        public static CompletionItem MarkItemToAlwaysFullyQualify(CompletionItem item) => item.WithProperties(item.Properties.Add(AlwaysFullyQualifyKey, AlwaysFullyQualifyKey));

        public static bool ShouldAlwaysFullyQualify(CompletionItem item) => item.Properties.ContainsKey(AlwaysFullyQualifyKey);
    }
}
