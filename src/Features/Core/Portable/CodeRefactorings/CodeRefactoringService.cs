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

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
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
                            .Select(grp => new KeyValuePair<string, Lazy<ImmutableArray<CodeRefactoringProvider>>>(
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
                    return ImmutableArray<CodeRefactoringProvider>.Empty;

                return ProjectCodeRefactoringProvider.GetExtensions(document, GetExtensionInfo);
            }

            static ProjectCodeRefactoringProvider.ExtensionInfo GetExtensionInfo(ExportCodeRefactoringProviderAttribute attribute)
                => new(attribute.DocumentKinds, attribute.DocumentExtensions);
        }

        public async Task<bool> HasRefactoringsAsync(
            TextDocument document,
            TextSpan state,
            CodeActionOptionsProvider options,
            CancellationToken cancellationToken)
        {
            var extensionManager = document.Project.Solution.Services.GetRequiredService<IExtensionManager>();

            foreach (var provider in GetProviders(document))
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefactoringToMetadataMap.TryGetValue(provider, out var providerMetadata);

                var refactoring = await GetRefactoringFromProviderAsync(
                    document, state, provider, providerMetadata, extensionManager, options, cancellationToken).ConfigureAwait(false);

                if (refactoring != null)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
            TextDocument document,
            TextSpan state,
            CodeActionRequestPriority priority,
            CodeActionOptionsProvider options,
            Func<string, IDisposable?> addOperationScope,
            CancellationToken cancellationToken)
        {
            using (TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeRefactoring_Summary, $"Pri{(int)priority}"))
            using (Logger.LogBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, cancellationToken))
            {
                var extensionManager = document.Project.Solution.Services.GetRequiredService<IExtensionManager>();
                using var _ = ArrayBuilder<Task<CodeRefactoring?>>.GetInstance(out var tasks);

                foreach (var provider in GetProviders(document))
                {
                    if (priority != CodeActionRequestPriority.None && priority != provider.RequestPriority)
                        continue;

                    tasks.Add(Task.Run(async () =>
                        {
                            // Log an individual telemetry event for slow code refactoring computations to
                            // allow targeted trace notifications for further investigation. 500 ms seemed like
                            // a good value so as to not be too noisy, but if fired, indicates a potential
                            // area requiring investigation.
                            const int CodeRefactoringTelemetryDelay = 500;

                            var providerName = provider.GetType().Name;
                            RefactoringToMetadataMap.TryGetValue(provider, out var providerMetadata);

                            using (addOperationScope(providerName))
                            using (RoslynEventSource.LogInformationalBlock(FunctionId.Refactoring_CodeRefactoringService_GetRefactoringsAsync, providerName, cancellationToken))
                            using (TelemetryLogging.LogBlockTime(FunctionId.CodeRefactoring_Delay, $"{providerName}", CodeRefactoringTelemetryDelay))
                            {
                                return await GetRefactoringFromProviderAsync(document, state, provider, providerMetadata,
                                    extensionManager, options, cancellationToken).ConfigureAwait(false);
                            }
                        },
                        cancellationToken));
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.WhereNotNull().ToImmutableArray();
            }
        }

        private async Task<CodeRefactoring?> GetRefactoringFromProviderAsync(
            TextDocument textDocument,
            TextSpan state,
            CodeRefactoringProvider provider,
            CodeChangeProviderMetadata? providerMetadata,
            IExtensionManager extensionManager,
            CodeActionOptionsProvider options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (extensionManager.IsDisabled(provider))
            {
                return null;
            }

            try
            {
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
            }
            catch (OperationCanceledException)
            {
                // We don't want to catch operation canceled exceptions in the catch block 
                // below. So catch is here and rethrow it.
                throw;
            }
            catch (Exception e)
            {
                extensionManager.HandleException(provider, e);
            }

            return null;
        }

        private class ProjectCodeRefactoringProvider
            : AbstractProjectExtensionProvider<ProjectCodeRefactoringProvider, CodeRefactoringProvider, ExportCodeRefactoringProviderAttribute>
        {
            protected override ImmutableArray<string> GetLanguages(ExportCodeRefactoringProviderAttribute exportAttribute)
                => exportAttribute.Languages.ToImmutableArray();

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
}
