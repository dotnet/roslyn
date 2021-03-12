// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions
{
    internal static class SymbolExtensions
    {
        public static async Task<IEnumerable<TaggedText>> GetDescriptionAsync(this ISymbol symbol, TextDocument document, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var codeProject = document.GetCodeProject();
            var formatter = codeProject.LanguageServices.GetService<IDocumentationCommentFormattingService>();
            if (formatter == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var symbolDisplayService = codeProject.LanguageServices.GetService<ISymbolDisplayService>();
            if (symbolDisplayService == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            // Any code document will do
            var codeDocument = codeProject.Documents.FirstOrDefault();
            if (codeDocument == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var semanticModel = await codeDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var description = await symbolDisplayService.ToDescriptionPartsAsync(codeProject.Solution.Workspace, semanticModel, 0, ImmutableArray.Create(symbol), SymbolDescriptionGroups.MainDescription, cancellationToken).ConfigureAwait(false);

            var builder = new List<TaggedText>();
            builder.AddRange(description.ToTaggedText());

            var documentation = symbol.GetDocumentationParts(semanticModel, 0, formatter, cancellationToken);
            if (documentation.Any())
            {
                builder.AddLineBreak();
                builder.AddRange(documentation);
            }

            var remarksDocumentation = symbol.GetRemarksDocumentationParts(semanticModel, 0, formatter, cancellationToken);
            if (remarksDocumentation.Any())
            {
                builder.AddLineBreak();
                builder.AddLineBreak();
                builder.AddRange(remarksDocumentation);
            }

            var returnsDocumentation = symbol.GetReturnsDocumentationParts(semanticModel, 0, formatter, cancellationToken);
            if (returnsDocumentation.Any())
            {
                builder.AddLineBreak();
                builder.AddLineBreak();
                builder.AddText(FeaturesResources.Returns_colon);
                builder.AddLineBreak();
                builder.Add(new TaggedText(TextTags.ContainerStart, "  "));
                builder.AddRange(returnsDocumentation);
                builder.Add(new TaggedText(TextTags.ContainerEnd, string.Empty));
            }

            var valueDocumentation = symbol.GetValueDocumentationParts(semanticModel, 0, formatter, cancellationToken);
            if (valueDocumentation.Any())
            {
                builder.AddLineBreak();
                builder.AddLineBreak();
                builder.AddText(FeaturesResources.Value_colon);
                builder.AddLineBreak();
                builder.Add(new TaggedText(TextTags.ContainerStart, "  "));
                builder.AddRange(valueDocumentation);
                builder.Add(new TaggedText(TextTags.ContainerEnd, string.Empty));
            }

            return builder;
        }
    }
}
