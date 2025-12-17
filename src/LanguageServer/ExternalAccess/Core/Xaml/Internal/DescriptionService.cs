// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

#pragma warning disable RS0030 // Do not use banned APIs
[Export(typeof(IDescriptionService))]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class DescriptionService : IDescriptionService
{
    private readonly IGlobalOptionService _globalOptions;

#pragma warning disable RS0030 // Do not use banned APIs
    [ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
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

            foreach (var part in section.TaggedParts)
            {
                if (part.Style == (TaggedTextStyle.Code | TaggedTextStyle.PreserveWhitespace) &&
                    builder.LastOrDefault().Tag != TextTags.CodeBlockStart)
                {
                    builder.Add(new TaggedText(TextTags.CodeBlockStart, string.Empty));
                    builder.Add(part);
                    builder.Add(new TaggedText(TextTags.CodeBlockEnd, string.Empty));
                }
                else
                {
                    builder.Add(part);
                }
            }
        }

        return builder.ToImmutableArray();
    }

    public (string content, bool isMarkdown) GetMarkupContent(ImmutableArray<TaggedText> tags, string language, bool featureSupportsMarkdown)
    {
        var markup = ProtocolConversions.GetDocumentationMarkupContent(tags, language, featureSupportsMarkdown);

        return (markup.Value, markup.Kind == LSP.MarkupKind.Markdown);
    }
}
