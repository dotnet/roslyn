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
        private const string GenericTypeNameManglingString = "`";
        private static readonly string[] s_aritySuffixesOneToNine = { "`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9" };

        private const string ContainingNamespaceName = nameof(ContainingNamespaceName);
        private const string TypeAritySuffixName = nameof(TypeAritySuffixName);

        public static CompletionItem Create(INamedTypeSymbol typeSymbol, string containingNamespace)
        {
            var builder = PooledDictionary<string, string>.GetInstance();
            builder.Add(ContainingNamespaceName, containingNamespace);

            if (typeSymbol.Arity > 0)
            {
                builder.Add(TypeAritySuffixName, GetAritySuffix(typeSymbol.Arity));
            }

            // TODO: Suffix should be language specific, i.e. `(Of ...)` if triggered from VB.
            return CompletionItem.CreateInternal(
                 displayText: typeSymbol.Name,
                 filterText: typeSymbol.Name,
                 sortText: typeSymbol.Name,
                 properties: builder.ToImmutableDictionaryAndFree(),
                 tags: GlyphTags.GetTags(typeSymbol.GetGlyph()),
                 rules: CompletionItemRules.Default,
                 displayTextPrefix: null,
                 displayTextSuffix: typeSymbol.Arity == 0 ? null : "<>",
                 inlineDescription: containingNamespace,
                 useEditorCompletionItemCache: true);
        }

        public static string GetContainingNamespace(CompletionItem item)
        {
            return item.Properties.TryGetValue(ContainingNamespaceName, out var containingNamespace)
                ? containingNamespace
                : null;
        }

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
            if (item.Properties.TryGetValue(ContainingNamespaceName, out var containingNamespace))
            {
                var fullyQualifiedName = GetFullyQualifiedName(containingNamespace, item.DisplayText);
                if (item.Properties.TryGetValue(TypeAritySuffixName, out var aritySuffix))
                {
                    return fullyQualifiedName + aritySuffix;
                }

                return fullyQualifiedName;
            }

            return null;
        }
    }
}
