// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.DebuggerCompletion;

/// <summary>
/// A brokered service that provides debugger completion results for a given document and expression context.
/// <see cref="GetDebuggerCompletionsAsync"/> is the main entry point.
/// </summary>
#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
[ExportBrokeredService(MonikerName, MonikerVersion, Audience = ServiceAudience.Local)]
internal sealed class DebuggerCompletionBrokeredService : IDebuggerCompletionService, IExportedBrokeredService
#pragma warning restore RS0030
{
    internal const string MonikerName = "Microsoft.CodeAnalysis.LanguageServer.DebuggerCompletionService";
    internal const string MonikerVersion = "0.1";
    private static readonly ServiceMoniker s_serviceMoniker = new(MonikerName, new Version(MonikerVersion));
    private static readonly ServiceRpcDescriptor s_serviceDescriptor = new ServiceJsonRpcDescriptor(
        s_serviceMoniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);

    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly ILogger _logger;

#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
    [ImportingConstructor]
#pragma warning restore RS0030
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DebuggerCompletionBrokeredService(
        LanguageServerWorkspaceFactory workspaceFactory,
        ILoggerFactory loggerFactory)
    {
        _workspaceFactory = workspaceFactory;
        _logger = loggerFactory.CreateLogger<DebuggerCompletionBrokeredService>();
    }

    public ServiceRpcDescriptor Descriptor => s_serviceDescriptor;

    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<DebuggerCompletionResult?> GetDebuggerCompletionsAsync(
        string documentFilePath,
        int statementEndLine,
        int statementEndCharacter,
        string expression,
        int cursorOffset,
        CancellationToken cancellationToken)
    {
        try
        {
            var workspace = _workspaceFactory.HostWorkspace;
            var solution = workspace.CurrentSolution;

            // Find the document by file path
            var documentIds = solution.GetDocumentIdsWithFilePath(documentFilePath);
            if (documentIds.IsEmpty)
            {
                _logger.LogWarning("Debugger completion: document not found for path {Path}", documentFilePath);
                return null;
            }

            var document = solution.GetRequiredDocument(documentIds.First());

            // Convert line/character to absolute position
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var contextPoint = sourceText.Lines.GetPosition(new LinePosition(statementEndLine, statementEndCharacter));

            // Get the language-specific splicer
            var splicer = document.Project.Services.GetService<IDebuggerSplicer>();
            if (splicer == null)
            {
                _logger.LogWarning("Debugger completion: splicer not available for language {Language}", document.Project.Language);
                return null;
            }

            // Validate cursorOffset
            if (cursorOffset < 0 || cursorOffset > expression.Length)
            {
                _logger.LogWarning("Debugger completion: invalid cursorOffset {Offset} for expression length {Length}", cursorOffset, expression.Length);
                return null;
            }

            // Splice the expression into the document
            var spliceResult = await splicer
                .SpliceAsync(document, contextPoint, expression, cursorOffset, cancellationToken)
                .ConfigureAwait(false);

            // Fork solution with spliced text
            var forkedSolution = solution.WithDocumentText(
                document.Id,
                spliceResult.Text,
                PreservationMode.PreserveIdentity);

            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                forkedSolution = forkedSolution.WithDocumentText(linkedDocumentId, spliceResult.Text, PreservationMode.PreserveIdentity);
            }

            var splicedDocument = forkedSolution.GetRequiredDocument(document.Id);

            // Get completions using the Roslyn completion service directly
            var completionService = splicedDocument.GetRequiredLanguageService<Completion.CompletionService>();
            var completions = await completionService.GetCompletionsAsync(
                splicedDocument,
                spliceResult.CompletionPosition,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (completions == null || completions.ItemsList.Count == 0)
            {
                return new DebuggerCompletionResult { Items = [] };
            }

            var items = completions.ItemsList.Select(item => new DebuggerCompletionResultItem
            {
                Label = item.DisplayText,
                SortText = item.SortText,
                FilterText = item.FilterText,
                InsertText = item.DisplayText,
            }).ToArray();

            return new DebuggerCompletionResult { Items = items };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debugger completion failed");
            return null;
        }
    }
}
