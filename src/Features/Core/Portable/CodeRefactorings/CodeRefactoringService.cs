// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

[Export(typeof(ICodeRefactoringService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CodeRefactoringService(
    [ImportMany] IEnumerable<Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>> providers) : ICodeRefactoringService
{
    private readonly Lazy<ImmutableDictionary<string, Lazy<ImmutableArray<CodeRefactoringProvider>>>> _lazyLanguageToProvidersMap = new Lazy<ImmutableDictionary<string, Lazy<ImmutableArray<CodeRefactoringProvider>>>>(
            () =>
                ImmutableDictionary.CreateRange(
                    DistributeLanguages(providers)
                        .GroupBy(lz => lz.Metadata.Language)
                        .Select(grp => KeyValuePairUtil.Create(
                            grp.Key,
                            new Lazy<ImmutableArray<CodeRefactoringProvider>>(() => ExtensionOrderer.Order(grp).Select(lz => lz.Value).ToImmutableArray())))));
    private readonly Lazy<ImmutableDictionary<CodeRefactoringProvider, CodeChangeProviderMetadata>> _lazyRefactoringToMetadataMap = new(() => providers.Where(provider => provider.IsValueCreated).ToImmutableDictionary(provider => provider.Value, provider => provider.Metadata));

    private ImmutableDictionary<CodeRefactoringProvider, FixAllProviderInfo?> _fixAllProviderMap = ImmutableDictionary<CodeRefactoringProvider, FixAllProviderInfo?>.Empty;

    private static IEnumerable<Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>> DistributeLanguages(IEnumerable<Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>> providers)
    {
        foreach (var provider in providers)
        {
            foreach (var language in provider.Metadata.Languages)
            {
                var orderable = new OrderableLanguageMetadata(
                    provider.Metadata.Name, language, provider.Metadata.AfterTyped, provider.Metadata.BeforeTyped);
                yield return new Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>(() => provider.Value, orderable);
            }
        }
    }

    private ImmutableDictionary<string, Lazy<ImmutableArray<CodeRefactoringProvider>>> LanguageToProvidersMap
        => _lazyLanguageToProvidersMap.Value;

    private ImmutableDictionary<CodeRefactoringProvider, CodeChangeProviderMetadata> RefactoringToMetadataMap
        => _lazyRefactoringToMetadataMap.Value;

    private ConcatImmutableArray<CodeRefactoringProvider> GetProviders(TextDocument document)
    {
        var allRefactorings = ImmutableArray<CodeRefactoringProvider>.Empty;
        if (LanguageToProvidersMap.TryGetValue(document.Project.Language, out var lazyProviders))
        {
            allRefactorings = ProjectCodeRefactoringProvider.FilterExtensions(document, lazyProviders.Value, GetExtensionInfo);
        }

        return allRefactorings.ConcatFast(GetProjectRefactorings(document));

        static ImmutableArray<CodeRefactoringProvider> GetProjectRefactorings(TextDocument document)
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict refactorings in Interactive
            if (document.Project.Solution.WorkspaceKind == WorkspaceKind.Interactive)
                return [];

            return ProjectCodeRefactoringProvider.GetExtensions(document, GetExtensionInfo);
        }

        static ProjectCodeRefactoringProvider.ExtensionInfo GetExtensionInfo(ExportCodeRefactoringProviderAttribute attribute)
        {
            var kinds = new TextDocumentKind[attribute.DocumentKinds.Length];
            for (var i = 0; i < kinds.Length; i++)
            {
                var kindString = attribute.DocumentKinds[i];
                if (!Enum.TryParse(kindString, out TextDocumentKind kind))
                    kind = 0;

                kinds[i] = kind;
            }

            return new(kinds, attribute.DocumentExtensions);
        }
    }

    public async Task<bool> HasRefactoringsAsync(
        TextDocument document,
        TextSpan state,
        CodeActionOptionsProvider options,
        CancellationToken cancellationToken)
    {
        // A token for controlling the inner work we do calling out to each provider.  Once we have a single provider
        // that returns a refactoring, we can cancel the work we're doing with all other providers.
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // This await will not complete until all providers have been called (though we'll attempt to bail out of any of
        // them once we have a single refactoring found).  So there's no concern here about produceItems running after
        // linkedTokenSource has been diposed.
        return await ProducerConsumer<VoidResult>.RunParallelAsync(
            source: this.GetProviders(document),
            produceItems: static async (provider, callback, args, cancellationToken) =>
            {
                var (@this, document, state, options, linkedTokenSource) = args;

                // Do no work if either the outer request canceled, or another provider already found a refactoring.
                if (cancellationToken.IsCancellationRequested || linkedTokenSource.Token.IsCancellationRequested)
                    return;

                try
                {
                    // We want to pass linkedTokenSource.Token here so that we can cancel the inner operation once the
                    // outer ProducerConsumer sees a single refactoring returned by any provider.
                    var refactoring = await @this.GetRefactoringFromProviderAsync(
                        document, state, provider, options, linkedTokenSource.Token).ConfigureAwait(false);

                    // If we have a refactoring, send a single VoidResult value to the consumer so it can cancel the
                    // other concurrent operations, and can return 'true' to the caller to indicate that there are
                    // refactorings.
                    if (refactoring != null)
                        callback(default(VoidResult));
                }
                catch (OperationCanceledException)
                {
                    // Ensure that the cancellation of the inner token doesn't bubble outside.  We are not canceling the
                    // entire operation just because one provider succeeded and canceled the rest.
                }
            },
            consumeItems: static async (items, args, cancellationToken) =>
            {
                // Try to consume from the results that produceItems is sending us.  The moment we get a single result,
                // we know we're done and we have at least one refactoring.
                await foreach (var unused in items)
                {
                    // Cancel all the other items that are still running (or are asked to run in the future).
                    args.linkedTokenSource.Cancel();
                    return true;
                }

                return false;
            },
            args: (this, document, state, options, linkedTokenSource),
            // intentionally using the outer token here.  The linked token is only used to cancel the inner operations.
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
        TextDocument document,
        TextSpan state,
        CodeActionRequestPriority? priority,
        CodeActionOptionsProvider options,
        CancellationToken cancellationToken)
    {
        using (TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeRefactoring_Summary, $"Pri{priority.GetPriorityInt()}"))
        using (Logger.LogBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, cancellationToken))
        {
            using var _ = PooledDictionary<CodeRefactoringProvider, int>.GetInstance(out var providerToIndex);

            var orderedProviders = GetProviders(document).Where(p => priority == null || p.RequestPriority == priority).ToImmutableArray();

            var pairs = await ProducerConsumer<(CodeRefactoringProvider provider, CodeRefactoring codeRefactoring)>.RunParallelAsync(
                source: orderedProviders,
                produceItems: static async (provider, callback, args, cancellationToken) =>
                {
                    var (@this, document, state, options) = args;

                    // Run all providers in parallel to get the set of refactorings for this document.
                    // Log an individual telemetry event for slow code refactoring computations to
                    // allow targeted trace notifications for further investigation. 500 ms seemed like
                    // a good value so as to not be too noisy, but if fired, indicates a potential
                    // area requiring investigation.
                    const int CodeRefactoringTelemetryDelay = 500;

                    var providerName = provider.GetType().Name;

                    var logMessage = KeyValueLogMessage.Create(m =>
                    {
                        m[TelemetryLogging.KeyName] = providerName;
                        m[TelemetryLogging.KeyLanguageName] = document.Project.Language;
                    });

                    using (RoslynEventSource.LogInformationalBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, providerName, cancellationToken))
                    using (TelemetryLogging.LogBlockTime(FunctionId.CodeRefactoring_Delay, logMessage, CodeRefactoringTelemetryDelay))
                    {
                        var refactoring = await @this.GetRefactoringFromProviderAsync(
                            document, state, provider, options, cancellationToken).ConfigureAwait(false);
                        if (refactoring != null)
                            callback((provider, refactoring));
                    }
                },
                args: (@this: this, document, state, options),
                cancellationToken).ConfigureAwait(false);

            // Order the refactorings by the order of the providers.
            foreach (var provider in orderedProviders)
                providerToIndex.Add(provider, providerToIndex.Count);

            return pairs
                .OrderBy((tuple1, tuple2) => providerToIndex[tuple1.provider] - providerToIndex[tuple2.provider])
                .SelectAsArray(t => t.codeRefactoring);
        }
    }

    private Task<CodeRefactoring?> GetRefactoringFromProviderAsync(
        TextDocument textDocument,
        TextSpan state,
        CodeRefactoringProvider provider,
        CodeActionOptionsProvider options,
        CancellationToken cancellationToken)
    {
        RefactoringToMetadataMap.TryGetValue(provider, out var providerMetadata);

        var extensionManager = textDocument.Project.Solution.Services.GetRequiredService<IExtensionManager>();

        return extensionManager.PerformFunctionAsync(
            provider,
            async cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var _ = ArrayBuilder<(CodeAction action, TextSpan? applicableToSpan)>.GetInstance(out var actions);
                var context = new CodeRefactoringContext(textDocument, state,

                    // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                    (action, applicableToSpan) =>
                    {
                        // Serialize access for thread safety - we don't know what thread the refactoring provider will call this delegate from.
                        lock (actions)
                        {
                            // Add the Refactoring Provider Name to the parent CodeAction's CustomTags.
                            // Always add a name even in cases of 3rd party refactorings that do not export
                            // name metadata.
                            action.AddCustomTagAndTelemetryInfo(providerMetadata, provider);

                            actions.Add((action, applicableToSpan));
                        }
                    },
                    options,
                    cancellationToken);

                var task = provider.ComputeRefactoringsAsync(context) ?? Task.CompletedTask;
                await task.ConfigureAwait(false);

                if (actions.Count == 0)
                {
                    return null;
                }

                var fixAllProviderInfo = extensionManager.PerformFunction(
                    provider, () => ImmutableInterlocked.GetOrAdd(ref _fixAllProviderMap, provider, FixAllProviderInfo.Create), defaultValue: null);
                return new CodeRefactoring(provider, actions.ToImmutable(), fixAllProviderInfo, options);
            }, defaultValue: null, cancellationToken);
    }

    private class ProjectCodeRefactoringProvider
        : AbstractProjectExtensionProvider<ProjectCodeRefactoringProvider, CodeRefactoringProvider, ExportCodeRefactoringProviderAttribute>
    {
        protected override ImmutableArray<string> GetLanguages(ExportCodeRefactoringProviderAttribute exportAttribute)
            => [.. exportAttribute.Languages];

        protected override bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<CodeRefactoringProvider> extensions)
        {
            // check whether the analyzer reference knows how to return fixers directly.
            if (reference is ICodeRefactoringProviderFactory codeRefactoringProviderFactory)
            {
                extensions = codeRefactoringProviderFactory.GetRefactorings();
                return true;
            }

            extensions = default;
            return false;
        }
    }
}
