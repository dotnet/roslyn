// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions
{
    internal static class SymbolExtensions
    {
        private static readonly SymbolDisplayFormat s_descriptionStyle =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword);

        public static async Task<IEnumerable<TaggedText>> GetDescriptionAsync(this ISymbol symbol, TextDocument document, int offset, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var formatter = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
            if (formatter == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            // TODO: Should we get this from the code-behind document instead?
            var codeDocument = document.Project.Documents.FirstOrDefault();
            if (codeDocument == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var semanticModel = await codeDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var textContentBuilder = new List<TaggedText>();
            textContentBuilder.AddRange(symbol.ToDisplayParts(s_descriptionStyle).ToTaggedText());

            var documentation = symbol.GetDocumentationParts(semanticModel, offset, formatter, cancellationToken);
            if (documentation.Any())
            {
                textContentBuilder.AddLineBreak();
                textContentBuilder.AddRange(documentation);
            }
            return textContentBuilder;
        }
    }
}
