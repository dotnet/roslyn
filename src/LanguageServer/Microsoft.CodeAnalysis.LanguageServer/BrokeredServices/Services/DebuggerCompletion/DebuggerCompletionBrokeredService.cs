// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.DebuggerCompletion;

[ExportCSharpVisualBasicLspServiceFactory(typeof(DebuggerCompletionBrokeredServiceContributor)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DebuggerCompletionBrokeredServiceContributorFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new DebuggerCompletionBrokeredServiceContributor(lspServices, lspServices.GetRequiredService<ILoggerFactory>());
}

internal sealed class DebuggerCompletionBrokeredServiceContributor(
    LspServices lspServices,
    ILoggerFactory loggerFactory) : IServiceBrokerInitializer, ILspService
{
    public ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister => new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { DebuggerCompletionBrokeredService.ServiceDescriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
    }.ToImmutableDictionary();

    public void Proffer(GlobalBrokeredServiceContainer container)
    {
        container.Proffer(
            DebuggerCompletionBrokeredService.ServiceDescriptor,
            async (moniker, options, innerServiceBroker, cancellationToken) =>
            {
                var workspaceFactory = lspServices.GetRequiredService<LanguageServerWorkspaceFactory>();
                var service = new DebuggerCompletionBrokeredService(workspaceFactory, loggerFactory);
                await service.InitializeAsync(cancellationToken).ConfigureAwait(false);
                return service;
            });
    }

    public void OnServiceBrokerInitialized(IServiceBroker serviceBroker, CancellationToken cancellationToken)
    {
    }
}

/// <summary>
/// A brokered service that provides debugger completion results for a given document and expression context.
/// <see cref="GetDebuggerCompletionsAsync"/> is the main entry point.
/// </summary>
internal sealed class DebuggerCompletionBrokeredService : IDebuggerCompletionService
{
    internal const string MonikerName = "Microsoft.CodeAnalysis.LanguageServer.DebuggerCompletionService";
    internal const string MonikerVersion = "0.1";
    private static readonly ServiceMoniker s_serviceMoniker = new(MonikerName, new Version(MonikerVersion));
    internal static readonly ServiceRpcDescriptor ServiceDescriptor = new ServiceJsonRpcDescriptor(
        s_serviceMoniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);

    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly ILogger _logger;

    public DebuggerCompletionBrokeredService(
        LanguageServerWorkspaceFactory workspaceFactory,
        ILoggerFactory loggerFactory)
    {
        _workspaceFactory = workspaceFactory;
        _logger = loggerFactory.CreateLogger<DebuggerCompletionBrokeredService>();
    }

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
            var forkedSolution = solution.WithDocumentText(document.Id, spliceResult.Text, PreservationMode.PreserveIdentity);
            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                forkedSolution = forkedSolution.WithDocumentText(linkedDocumentId, spliceResult.Text, PreservationMode.PreserveIdentity);
            }

            var splicedDocument = forkedSolution.GetRequiredDocument(document.Id);

            // Use debugger-specific completion options, matching the Visual Studio debugger completion path.
            var completionOptions = Completion.CompletionOptions.Default.WithDebuggerOverrides();

            var completionService = splicedDocument.GetRequiredLanguageService<Completion.CompletionService>();
            var completions = await completionService.GetCompletionsAsync(
                splicedDocument,
                spliceResult.CompletionPosition,
                completionOptions,
                splicedDocument.Project.Solution.Options,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (completions == null || completions.ItemsList.Count == 0)
            {
                return new DebuggerCompletionResult { Items = [] };
            }

            var items = completions.ItemsList.Select((item, index) => new DebuggerCompletionResultItem
            {
                Label = item.DisplayText,
                SortText = GetDebuggerSortText(item, index),
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

    private static string GetDebuggerSortText(Completion.CompletionItem item, int index)
    {
        var matchPriorityBucket = int.MaxValue - (long)item.Rules.MatchPriority;
        return $"{matchPriorityBucket:D10}{GetSymbolKindBucket(item):D1}{index:D4}{item.SortText}";

        static int GetSymbolKindBucket(Completion.CompletionItem item)
        {
            return SymbolCompletionItem.GetKind(item) switch
            {
                SymbolKind.Local or SymbolKind.Parameter or SymbolKind.RangeVariable => 0,
                SymbolKind.Field or SymbolKind.Property => 1,
                SymbolKind.Event or SymbolKind.Method => 2,
                SymbolKind.NamedType => 3,
                _ => 4,
            };
        }
    }

    internal readonly struct TestAccessor
    {
        internal static string GetDebuggerSortText(Completion.CompletionItem item, int index)
            => DebuggerCompletionBrokeredService.GetDebuggerSortText(item, index);
    }
}
