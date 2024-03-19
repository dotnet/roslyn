// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion;

public abstract partial class CompletionService
{
    private sealed class ProviderManager : IEqualityComparer<ImmutableHashSet<string>>
    {
        private readonly object _gate = new();
        private readonly Lazy<ImmutableDictionary<string, CompletionProvider>> _nameToProvider;
        private readonly Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>> _rolesToProviders;
        private IReadOnlyList<Lazy<CompletionProvider, CompletionProviderMetadata>>? _lazyImportedProviders;
        private readonly CompletionService _service;

        private readonly AsyncBatchingWorkQueue<IReadOnlyList<AnalyzerReference>> _projectProvidersWorkQueue;

        public ProviderManager(CompletionService service, IAsynchronousOperationListenerProvider listenerProvider)
        {
            _service = service;
            _rolesToProviders = new Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>>(this);
            _nameToProvider = new Lazy<ImmutableDictionary<string, CompletionProvider>>(LoadImportedProvidersAndCreateNameMap, LazyThreadSafetyMode.PublicationOnly);

            _projectProvidersWorkQueue = new AsyncBatchingWorkQueue<IReadOnlyList<AnalyzerReference>>(
                    TimeSpan.FromSeconds(1),
                    ProcessBatchAsync,
                    EqualityComparer<IReadOnlyList<AnalyzerReference>>.Default,
                    listenerProvider.GetListener(FeatureAttribute.CompletionSet),
                    CancellationToken.None);
        }

        private ImmutableDictionary<string, CompletionProvider> LoadImportedProvidersAndCreateNameMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, CompletionProvider>();

            foreach (var lazyImportedProvider in GetLazyImportedProviders())
                builder[lazyImportedProvider.Value.Name] = lazyImportedProvider.Value;

#pragma warning disable CS0618
            // We need to keep supporting built-in providers for a while longer since this is a public API.
            foreach (var builtinProvider in _service.GetBuiltInProviders())
                builder[builtinProvider.Name] = builtinProvider;
#pragma warning restore CS0618

            return builder.ToImmutable();
        }

        private IReadOnlyList<Lazy<CompletionProvider, CompletionProviderMetadata>> GetLazyImportedProviders()
        {
            if (_lazyImportedProviders == null)
            {
                var language = _service.Language;
                var mefExporter = _service._services.ExportProvider;

                var providers = ExtensionOrderer.Order(
                        mefExporter.GetExports<CompletionProvider, CompletionProviderMetadata>()
                        .Where(lz => lz.Metadata.Language == language)
                        ).ToList();

                Interlocked.CompareExchange(ref _lazyImportedProviders, providers, null);
            }

            return _lazyImportedProviders;
        }

        private ValueTask ProcessBatchAsync(ImmutableSegmentedList<IReadOnlyList<AnalyzerReference>> referencesList, CancellationToken cancellationToken)
        {
            foreach (var references in referencesList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Go through the potentially slow path to ensure project providers are loaded.
                // We only do this in background here to avoid UI delays.
                _ = ProjectCompletionProvider.GetExtensions(_service.Language, references);
            }

            return ValueTaskFactory.CompletedTask;
        }

        public ImmutableArray<CompletionProvider> GetCachedProjectCompletionProvidersOrQueueLoadInBackground(Project? project, CompletionOptions options)
        {
            if (project is null || project.Solution.WorkspaceKind == WorkspaceKind.Interactive)
            {
                // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict completions in Interactive
                return [];
            }

            // On primary completion paths, don't load providers if they are not already cached,
            // return immediately and load them in background instead. If the test hook
            // 'ForceExpandedCompletionIndexCreation' is set, calculate the values immediately.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1620947
            if (options.ForceExpandedCompletionIndexCreation)
            {
                return ProjectCompletionProvider.GetExtensions(_service.Language, project.AnalyzerReferences);
            }

            if (ProjectCompletionProvider.TryGetCachedExtensions(project.AnalyzerReferences, out var providers))
                return providers;

            _projectProvidersWorkQueue.AddWork(project.AnalyzerReferences);
            return [];
        }

        private ImmutableArray<CompletionProvider> GetImportedAndBuiltInProviders(ImmutableHashSet<string>? roles)
        {
            roles ??= [];

            lock (_gate)
            {
                if (!_rolesToProviders.TryGetValue(roles, out var providers))
                {
                    providers = GetImportedAndBuiltInProvidersWorker(roles);
                    _rolesToProviders.Add(roles, providers);
                }

                return providers;
            }

            ImmutableArray<CompletionProvider> GetImportedAndBuiltInProvidersWorker(ImmutableHashSet<string> roles)
            {
                return
                [
                    .. GetLazyImportedProviders()
                        .Where(lz => lz.Metadata.Roles == null || lz.Metadata.Roles.Length == 0 || roles.Overlaps(lz.Metadata.Roles))
                        .Select(lz => lz.Value),
                    // We need to keep supporting built-in providers for a while longer since this is a public API.
                    // https://github.com/dotnet/roslyn/issues/42367
#pragma warning disable 0618
                    .. _service.GetBuiltInProviders(),
#pragma warning restore 0618
                ];
            }
        }

        public CompletionProvider? GetProvider(CompletionItem item, Project? project)
        {
            if (item.ProviderName == null)
                return null;

            if (_nameToProvider.Value.TryGetValue(item.ProviderName, out var provider))
                return provider;

            using var _ = PooledDelegates.GetPooledFunction(static (p, n) => p.Name == n, item.ProviderName, out Func<CompletionProvider, bool> isNameMatchingProviderPredicate);

            // Publicly available options will not impact this call, since the completion item must have already
            // existed if it produced the input completion item.
            return GetCachedProjectCompletionProvidersOrQueueLoadInBackground(project, CompletionOptions.Default).FirstOrDefault(isNameMatchingProviderPredicate);
        }

        public ConcatImmutableArray<CompletionProvider> GetFilteredProviders(
            Project? project, ImmutableHashSet<string>? roles, CompletionTrigger trigger, in CompletionOptions options)
        {
            var allCompletionProviders = FilterProviders(GetImportedAndBuiltInProviders(roles), trigger, options);
            var projectCompletionProviders = FilterProviders(GetCachedProjectCompletionProvidersOrQueueLoadInBackground(project, options), trigger, options);
            return allCompletionProviders.ConcatFast(projectCompletionProviders);
        }

        private ImmutableArray<CompletionProvider> FilterProviders(
            ImmutableArray<CompletionProvider> providers,
            CompletionTrigger trigger,
            in CompletionOptions options)
        {
            providers = options.ExpandedCompletionBehavior switch
            {
                ExpandedCompletionMode.NonExpandedItemsOnly => providers.WhereAsArray(p => !p.IsExpandItemProvider),
                ExpandedCompletionMode.ExpandedItemsOnly => providers.WhereAsArray(p => p.IsExpandItemProvider),
                _ => providers,
            };

            // If the caller passed along specific options that affect snippets,
            // then defer to those.  Otherwise if the caller just wants the default
            // behavior, then get the snippets behavior from our own rules.
            var snippetsRule = options.SnippetsBehavior != SnippetsRule.Default
                ? options.SnippetsBehavior
                : _service.GetRules(options).SnippetsRule;

            if (snippetsRule is SnippetsRule.Default or
                SnippetsRule.NeverInclude)
            {
                return providers.Where(p => !p.IsSnippetProvider).ToImmutableArray();
            }
            else if (snippetsRule == SnippetsRule.AlwaysInclude)
            {
                return providers;
            }
            else if (snippetsRule == SnippetsRule.IncludeAfterTypingIdentifierQuestionTab)
            {
                if (trigger.Kind == CompletionTriggerKind.Snippets)
                {
                    return providers.Where(p => p.IsSnippetProvider).ToImmutableArray();
                }
                else
                {
                    return providers.Where(p => !p.IsSnippetProvider).ToImmutableArray();
                }
            }

            return [];
        }

        public void LoadProviders()
        {
            _ = _nameToProvider.Value;
        }

        bool IEqualityComparer<ImmutableHashSet<string>>.Equals([AllowNull] ImmutableHashSet<string> x, [AllowNull] ImmutableHashSet<string> y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null || x.Count != y.Count)
            {
                return false;
            }

            foreach (var v in x)
            {
                if (!y.Contains(v))
                {
                    return false;
                }
            }

            return true;
        }

        int IEqualityComparer<ImmutableHashSet<string>>.GetHashCode([DisallowNull] ImmutableHashSet<string> obj)
        {
            var hash = 0;
            foreach (var o in obj)
            {
                hash += o.GetHashCode();
            }

            return hash;
        }

        private sealed class ProjectCompletionProvider
            : AbstractProjectExtensionProvider<ProjectCompletionProvider, CompletionProvider, ExportCompletionProviderAttribute>
        {
            protected override ImmutableArray<string> GetLanguages(ExportCompletionProviderAttribute exportAttribute)
                => [exportAttribute.Language];

            protected override bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<CompletionProvider> extensions)
            {
                // check whether the analyzer reference knows how to return completion providers directly.
                if (reference is ICompletionProviderFactory completionProviderFactory)
                {
                    extensions = completionProviderFactory.GetCompletionProviders();
                    return true;
                }

                extensions = default;
                return false;
            }
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor(ProviderManager providerManager)
        {
            private readonly ProviderManager _providerManager = providerManager;

            public ImmutableArray<CompletionProvider> GetImportedAndBuiltInProviders(ImmutableHashSet<string> roles)
            {
                return _providerManager.GetImportedAndBuiltInProviders(roles);
            }

            public ImmutableArray<CompletionProvider> GetProjectProviders(Project project)
            {
                // Force-load the extension completion providers
                return _providerManager.GetCachedProjectCompletionProvidersOrQueueLoadInBackground(
                    project,
                    CompletionOptions.Default with { ForceExpandedCompletionIndexCreation = true });
            }
        }
    }
}
