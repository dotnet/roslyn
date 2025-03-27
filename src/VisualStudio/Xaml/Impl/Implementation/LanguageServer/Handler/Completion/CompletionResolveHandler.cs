// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;
using Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions;
using Newtonsoft.Json.Linq;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler;

/// <summary>
/// Handle a completion resolve request to add description.
/// </summary>
[ExportStatelessXamlLspService(typeof(CompletionResolveHandler)), Shared]
[Method(LSP.Methods.TextDocumentCompletionResolveName)]
internal sealed class CompletionResolveHandler : ILspServiceRequestHandler<LSP.CompletionItem, LSP.CompletionItem>
{
    private readonly IGlobalOptionService _globalOptions;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CompletionResolveHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public async Task<LSP.CompletionItem> HandleRequestAsync(LSP.CompletionItem completionItem, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        if (completionItem is not VSInternalCompletionItem vsCompletionItem)
        {
            return completionItem;
        }

        CompletionResolveData? data;
        if (completionItem.Data is JToken token)
        {
            data = token.ToObject<CompletionResolveData>();
            Assumes.Present(data);
        }
        else
        {
            return completionItem;
        }

        var documentId = DocumentId.CreateFromSerialized(ProjectId.CreateFromSerialized(data.ProjectGuid), data.DocumentGuid);
        var document = context.Solution.GetDocument(documentId) ?? context.Solution.GetAdditionalDocument(documentId);
        if (document == null)
        {
            return completionItem;
        }

        var offset = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(data.Position), cancellationToken).ConfigureAwait(false);
        var completionService = document.Project.Services.GetRequiredService<IXamlCompletionService>();
        var symbol = await completionService.GetSymbolAsync(new XamlCompletionContext(document, offset), completionItem.Label, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (symbol == null)
        {
            return completionItem;
        }

        var options = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
        var description = await symbol.GetDescriptionAsync(document, options, cancellationToken).ConfigureAwait(false);

        vsCompletionItem.Description = new ClassifiedTextElement(description.Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
        return vsCompletionItem;
    }
}
