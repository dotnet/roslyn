// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotChangeAnalysisService : IWorkspaceService
{
    /// <summary>
    /// Kicks of work to analyze a change that copilot suggested making to a document. <paramref name="document"/> is
    /// the state of the document prior to the edits, and <paramref name="changes"/> are the changes Copilot wants to
    /// make to it.  <paramref name="changes"/> must be sorted and normalized before calling this.
    /// </summary>
    Task<CopilotChangeAnalysis> AnalyzeChangeAsync(
        Document document, ImmutableArray<TextChange> changes, string proposalId, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ICopilotChangeAnalysisService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultCopilotChangeAnalysisService(
    [Import(AllowDefault = true)] ICodeFixService? codeFixService = null) : ICopilotChangeAnalysisService
{
#pragma warning disable IDE0052 // Remove unread private members
    private readonly ICodeFixService? _codeFixService = codeFixService;
#pragma warning restore IDE0052 // Remove unread private members

    public async Task<CopilotChangeAnalysis> AnalyzeChangeAsync(
        Document document,
        ImmutableArray<TextChange> changes,
        string proposalId,
        CancellationToken cancellationToken)
    {
        if (!document.SupportsSemanticModel)
            return default;

        Contract.ThrowIfTrue(!changes.IsSorted(static (c1, c2) => c1.Span.Start - c2.Span.Start), "'changes' was not sorted.");
        Contract.ThrowIfTrue(new NormalizedTextSpanCollection(changes.Select(c => c.Span)).Count != changes.Length, "'changes' was not normalized.");

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);

        if (client != null)
        {
            var value = await client.TryInvokeAsync<IRemoteCopilotChangeAnalysisService, CopilotChangeAnalysis>(
                // Don't need to sync the entire solution over.  Just the cone of projects this document it contained within.
                document.Project,
                (service, checksum, cancellationToken) => service.AnalyzeChangeAsync(
                    checksum, document.Id, changes, proposalId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return value.HasValue ? value.Value : default;
        }
        else
        {
            return await AnalyzeChangeInCurrentProcessAsync(document, changes, cancellationToken).ConfigureAwait(false);
        }
    }

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<CopilotChangeAnalysis> AnalyzeChangeInCurrentProcessAsync(
        Document document,
        ImmutableArray<TextChange> changes,
        CancellationToken cancellationToken)
    {
        return default;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1822 // Mark members as static
}
