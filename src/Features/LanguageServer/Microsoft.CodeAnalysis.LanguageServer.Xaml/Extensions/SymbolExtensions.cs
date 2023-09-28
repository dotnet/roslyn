// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Extensions;

internal static class SymbolExtensions
{
    public static async Task<IEnumerable<TaggedText>> GetDescriptionAsync(this ISymbol symbol, TextDocument document, SymbolDescriptionOptions options, CancellationToken cancellationToken)
    {
        if (symbol is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var codeProject = document.Project;
        var formatter = codeProject.Services.GetService<IDocumentationCommentFormattingService>();
        if (formatter is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var symbolDisplayService = codeProject.Services.GetService<ISymbolDisplayService>();
        if (symbolDisplayService is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        // Any code document will do
        var codeDocument = codeProject.Documents.FirstOrDefault();
        if (codeDocument is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var semanticModel = await codeDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var services = codeProject.Solution.Services;
        var quickInfo = await QuickInfoUtilities.CreateQuickInfoItemAsync(services, semanticModel, span: default, ImmutableArray.Create(symbol), options, cancellationToken).ConfigureAwait(false);
        var builder = new List<TaggedText>();
        foreach (var section in quickInfo.Sections)
        {
            if (builder.Any())
            {
                builder.AddLineBreak();
            }

            builder.AddRange(section.TaggedParts);
        }

        return builder.ToImmutableArray();
    }
}
