// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractPreprocessorCompletionProvider : LSPCompletionProvider
{
    public sealed override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var originatingDocument = context.Document;
        var position = context.Position;
        var solution = originatingDocument.Project.Solution;
        var syntaxContext = await context.GetSyntaxContextWithExistingSpeculativeModelAsync(originatingDocument, cancellationToken).ConfigureAwait(false);
        if (!syntaxContext.IsPreProcessorExpressionContext)
            return;

        // Walk all the projects this document is linked in so that we get the full set of preprocessor symbols
        // defined across all of them.
        var syntaxFacts = originatingDocument.GetRequiredLanguageService<ISyntaxFactsService>();
        var preprocessorNames = new HashSet<string>(syntaxFacts.StringComparer);

        foreach (var documentId in solution.GetRelatedDocumentIds(originatingDocument.Id))
        {
            var document = solution.GetRequiredDocument(documentId);
            var currentSyntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            preprocessorNames.AddRange(currentSyntaxTree.Options.PreprocessorSymbolNames);
        }

        // Keep all the preprocessor symbol names together.  We don't want to intermingle them with any keywords we
        // include (like `true/false`)
        foreach (var name in preprocessorNames.OrderBy(a => a))
        {
            context.AddItem(CommonCompletionItem.Create(
                name,
                displayTextSuffix: "",
                CompletionItemRules.Default,
                glyph: Glyph.Keyword,
                sortText: "_0_" + name));
        }
    }
}
