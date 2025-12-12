// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Copilot;

[Method(MethodName)]
[ExportCSharpVisualBasicStatelessLspService(typeof(CopilotCompletionResolveContextHandler), WellKnownLspServerKinds.Any), Shared]
[method: ImportingConstructor]
[method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
internal sealed class CopilotCompletionResolveContextHandler(ICSharpCopilotContextProviderService contextProviderService)
    : ILspServiceDocumentRequestHandler<ContextResolveParam, IContextItem[]>
{
    // "@2" prefix to differentiate it from the implementation previously located in devkit extension.
    private const string MethodName = "roslyn/resolveContext@2";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public ICSharpCopilotContextProviderService ContextProviderService { get; } = contextProviderService;

    public TextDocumentIdentifier GetTextDocumentIdentifier(ContextResolveParam request)
        => request.DocumentContext.TextDocument;

    public async Task<IContextItem[]> HandleRequestAsync(ContextResolveParam param, RequestContext context, CancellationToken cancellationToken)
    {
        var linePosition = new LinePosition(param.DocumentContext.Position.Line, param.DocumentContext.Position.Character);
        var document = context.GetRequiredDocument();

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var position = text.Lines.GetPosition(linePosition);
        var activeExperiments = param.GetUnpackedActiveExperiments();

        using var _ = ArrayBuilder<IContextItem>.GetInstance(out var builder);
        await foreach (var item in ContextProviderService.GetContextItemsAsync(document, position, activeExperiments, cancellationToken).ConfigureAwait(false))
            builder.Add(item);

        return builder.ToArray();
    }
}
