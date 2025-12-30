// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

internal sealed class SemanticSearchQueryExecutor(
    FindUsagesContext presenterContext,
    Action<string> logMessage,
    IOptionsReader options)
{
    private sealed class ResultsObserver(IFindUsagesContext presenterContext, IOptionsReader options, Action<string> logMessage, Document? queryDocument) : ISemanticSearchResultsDefinitionObserver
    {
        private readonly Lazy<ConcurrentStack<(DocumentId documentId, ImmutableArray<TextChange> changes)>> _lazyDocumentUpdates = new();
        private readonly Lazy<ConcurrentDictionary<string, string?>> _lazyTextFileUpdates = new();

        public async ValueTask<ClassificationOptions> GetClassificationOptionsAsync(Microsoft.CodeAnalysis.Host.LanguageServices language, CancellationToken cancellationToken)
            => options.GetClassificationOptions(language.Language);

        public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
            => presenterContext.OnDefinitionFoundAsync(definition, cancellationToken);

        public ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
            => presenterContext.ProgressTracker.AddItemsAsync(itemCount, cancellationToken);

        public ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
            => presenterContext.ProgressTracker.ItemsCompletedAsync(itemCount, cancellationToken);

        public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
            => presenterContext.OnDefinitionFoundAsync(
                new SearchExceptionDefinitionItem(exception.Message, exception.TypeName, exception.StackTrace, (queryDocument != null) ? new DocumentSpan(queryDocument, exception.Span) : default), cancellationToken);

        public async ValueTask OnLogMessageAsync(string message, CancellationToken cancellationToken)
        {
            logMessage(message);
        }

        public async ValueTask OnDocumentUpdatedAsync(DocumentId documentId, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
        {
            _lazyDocumentUpdates.Value.Push((documentId, changes));
        }

        private ImmutableArray<(DocumentId documentId, ImmutableArray<TextChange> changes)> GetDocumentUpdates()
            => _lazyDocumentUpdates.IsValueCreated ? [.. _lazyDocumentUpdates.Value] : [];

        public async ValueTask<Solution> GetUpdatedSolutionAsync(Solution oldSolution, CancellationToken cancellationToken)
        {
            var newSolution = oldSolution;

            foreach (var (documentId, changes) in GetDocumentUpdates())
            {
                var oldText = await newSolution.GetRequiredDocument(documentId).GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (changes.IsEmpty)
                {
                    newSolution = newSolution.RemoveDocument(documentId);
                }
                else
                {
                    // TODO: auto-format/clean up changed spans
                    newSolution = newSolution.WithDocumentText(documentId, oldText.WithChanges(changes));

                    var newDocument = newSolution.GetRequiredDocument(documentId);
                    var organizeImportsService = newDocument.GetRequiredLanguageService<IOrganizeImportsService>();
                    var options = await newDocument.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
                    newDocument = await organizeImportsService.OrganizeImportsAsync(newDocument, options, cancellationToken).ConfigureAwait(false);
                    var updatedText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    newSolution = newSolution.WithDocumentText(newDocument.Id, updatedText);
                }
            }

            return newSolution;
        }

        public ImmutableArray<(string filePath, string? newContent)> GetFileUpdates()
            => _lazyTextFileUpdates.IsValueCreated ? _lazyTextFileUpdates.Value.SelectAsArray(static entry => (entry.Key, entry.Value)) : [];

        public async ValueTask OnTextFileUpdatedAsync(string filePath, string? newContent, CancellationToken cancellationToken)
        {
            _lazyTextFileUpdates.Value.TryAdd(filePath, newContent);
        }
    }

    public async Task<(Solution solution, ImmutableArray<(string filePath, string? newContent)> fileUpdates)> ExecuteAsync(string? query, Document? queryDocument, Solution solution, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(query is null ^ queryDocument is null);

        if (solution.ProjectIds is [])
        {
            try
            {
                await presenterContext.ReportNoResultsAsync(ServicesVSResources.Search_found_no_results_no_csharp_or_vb_projects_opened, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Notify the presenter even if the search has been cancelled.
                await presenterContext.OnCompletedAsync(CancellationToken.None).ConfigureAwait(false);
            }

            return (solution, []);
        }

        var resultsObserver = new ResultsObserver(presenterContext, options, logMessage, queryDocument);
        query ??= (await queryDocument!.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();

        ExecuteQueryResult result = default;
        var canceled = false;
        var emitTime = TimeSpan.Zero;

        try
        {
            var compileResult = await RemoteSemanticSearchServiceProxy.CompileQueryAsync(
                solution.Services,
                query,
                targetLanguage: null,
                cancellationToken).ConfigureAwait(false);

            if (compileResult == null)
            {
                result = new ExecuteQueryResult(FeaturesResources.Semantic_search_only_supported_on_net_core);
                return (solution, []);
            }

            emitTime = compileResult.Value.EmitTime;

            if (!compileResult.Value.CompilationErrors.IsEmpty)
            {
                foreach (var error in compileResult.Value.CompilationErrors)
                {
                    await presenterContext.OnDefinitionFoundAsync(new SearchCompilationFailureDefinitionItem(error, queryDocument), cancellationToken).ConfigureAwait(false);
                }

                return (solution, []);
            }

            result = await RemoteSemanticSearchServiceProxy.ExecuteQueryAsync(
                solution,
                compileResult.Value.QueryId,
                resultsObserver,
                new QueryExecutionOptions(),
                cancellationToken).ConfigureAwait(false);

            // apply document changes:
            var newSolution = await resultsObserver.GetUpdatedSolutionAsync(solution, cancellationToken).ConfigureAwait(false);

            return (newSolution, resultsObserver.GetFileUpdates());
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            result = new ExecuteQueryResult(e.Message);
        }
        catch (OperationCanceledException)
        {
            result = new ExecuteQueryResult(ServicesVSResources.Search_cancelled);
            canceled = true;
        }
        finally
        {
            var errorMessage = result.ErrorMessage;

            if (errorMessage != null)
            {
                if (result.ErrorMessageArgs != null)
                {
                    errorMessage = string.Format(errorMessage, result.ErrorMessageArgs);
                }

                // not cancellable since we might be reporting cancellation:
                await presenterContext.ReportMessageAsync(
                    errorMessage,
                    canceled ? NotificationSeverity.Information : NotificationSeverity.Error,
                    CancellationToken.None).ConfigureAwait(false);
            }

            // Notify the presenter even if the search has been cancelled.
            await presenterContext.OnCompletedAsync(CancellationToken.None).ConfigureAwait(false);

            ReportTelemetry(query, result, emitTime, canceled);
        }

        return (solution, []);
    }

    private static void ReportTelemetry(string queryString, ExecuteQueryResult result, TimeSpan emitTime, bool canceled)
    {
        Logger.Log(FunctionId.SemanticSearch_QueryExecution, KeyValueLogMessage.Create(map =>
        {
            map["Query"] = new PiiValue(queryString);

            if (canceled)
            {
                map["Canceled"] = true;
            }
            else if (result.ErrorMessage != null)
            {
                map["ErrorMessage"] = result.ErrorMessage;

                if (result.ErrorMessageArgs != null)
                {
                    map["ErrorMessageArgs"] = new PiiValue(string.Join("|", result.ErrorMessageArgs));
                }
            }

            map["ExecutionTimeMilliseconds"] = (long)result.ExecutionTime.TotalMilliseconds;
            map["EmitTime"] = (long)emitTime.TotalMilliseconds;
        }));
    }
}
