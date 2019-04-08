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
        private static readonly string[] s_IntegerOneToNine = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private static readonly string[] s_aritySuffixesOneToNine = { "`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9" };

        private const string ContainingNamespaceName = nameof(ContainingNamespaceName);
        private const string OverloadCountName = nameof(OverloadCountName);
        private const string TypeArityName = nameof(TypeArityName);

        public static CompletionItem Create(INamedTypeSymbol typeSymbol, string containingNamespace, int overloadCount)
        {
            var builder = PooledDictionary<string, string>.GetInstance();
            builder.Add(ContainingNamespaceName, containingNamespace);

            if (overloadCount > 0)
            {
                builder.Add(OverloadCountName, GetInteger(overloadCount));
            }

            if (typeSymbol.Arity > 0)
            {
                builder.Add(TypeArityName, GetInteger(typeSymbol.Arity));
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
                    var overloadCount = item.Properties.TryGetValue(TypeImportCompletionItem.OverloadCountName, out var count)
                        ? int.Parse(count)
                        : 0;

                    return await CommonCompletionUtilities.CreateDescriptionAsync(
                        document.Project.Solution.Workspace,
                        semanticModel,
                        position: 0,
                        symbol,
                        overloadCount: overloadCount,
                        supportedPlatforms: null,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        private static string GetInteger(int integer)
        {
            Debug.Assert(integer > 0);
            return (integer <= s_IntegerOneToNine.Length)
                ? s_IntegerOneToNine[integer - 1]
                : integer.ToString(CultureInfo.InvariantCulture);
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

        private static string ComposeAritySuffixedMetadataName(string name, int arity)
            => arity == 0 ? name : name + GetAritySuffix(arity);

        private static string GetMetadataName(CompletionItem item)
        {
            if (item.Properties.TryGetValue(ContainingNamespaceName, out var containingNamespace))
            {
                var arity = item.Properties.TryGetValue(TypeArityName, out var arityString) ? int.Parse(arityString) : 0;
                return ComposeAritySuffixedMetadataName(GetFullyQualifiedName(containingNamespace, item.DisplayText), arity);
            }

            return null;
        }
    }
}
