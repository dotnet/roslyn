// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.QuickInfo;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

[Export(typeof(IDescriptionService))]
internal class DescriptionService : IDescriptionService
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DescriptionService(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public async Task<IEnumerable<TaggedText>> GetDescriptionAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
    {
        if (symbol is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var formatter = project.Services.GetService<IDocumentationCommentFormattingService>();
        if (formatter is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var symbolDisplayService = project.Services.GetService<ISymbolDisplayService>();
        if (symbolDisplayService is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        // Any code document will do
        var codeDocument = project.Documents.FirstOrDefault();
        if (codeDocument is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var semanticModel = await codeDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return Enumerable.Empty<TaggedText>();
        }

        var services = project.Solution.Services;
        var options = _globalOptions.GetSymbolDescriptionOptions(project.Language);
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

    public MarkupContent GetMarkupContent(ImmutableArray<TaggedText> tags, string language, bool featureSupportsMarkdown)
        => ProtocolConversions.GetDocumentationMarkupContent(tags, language, featureSupportsMarkdown);
}
