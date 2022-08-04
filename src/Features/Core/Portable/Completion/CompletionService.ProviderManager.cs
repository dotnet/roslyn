// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    public abstract partial class CompletionService
    {
        private sealed class ProviderManager : IEqualityComparer<ImmutableHashSet<string>>
        {
            private readonly object _gate = new();
            private readonly Dictionary<string, CompletionProvider?> _nameToProvider = new();
            private readonly Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>> _rolesToProviders;

            private IReadOnlyList<Lazy<CompletionProvider, CompletionProviderMetadata>>? _lazyImportedProviders;
            private readonly CompletionService _service;

            public ProviderManager(CompletionService service)
            {
                _service = service;
                _rolesToProviders = new Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>>(this);
            }

            public IReadOnlyList<Lazy<CompletionProvider, CompletionProviderMetadata>> GetLazyImportedProviders()
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

            public static ImmutableArray<CompletionProvider> GetProjectCompletionProviders(Project? project)
            {
                if (project is null || project.Solution.WorkspaceKind == WorkspaceKind.Interactive)
                {
                    // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict completions in Interactive
                    return ImmutableArray<CompletionProvider>.Empty;
                }

                return ProjectCompletionProvider.GetExtensions(project);
            }

            private ImmutableArray<CompletionProvider> GetImportedAndBuiltInProviders(ImmutableHashSet<string>? roles)
            {
                roles ??= ImmutableHashSet<string>.Empty;

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
                    using var _ = ArrayBuilder<CompletionProvider>.GetInstance(out var providers);
                    providers.AddRange(GetLazyImportedProviders()
                        .Where(lz => lz.Metadata.Roles == null || lz.Metadata.Roles.Length == 0 || roles.Overlaps(lz.Metadata.Roles))
                        .Select(lz => lz.Value));

#pragma warning disable 0618
                    // We need to keep supporting built-in providers for a while longer since this is a public API.
                    // https://github.com/dotnet/roslyn/issues/42367
                    providers.AddRange(_service.GetBuiltInProviders());
#pragma warning restore 0618

                    return providers.ToImmutable();
                }
            }

            public CompletionProvider? GetProvider(CompletionItem item, Project? project)
            {
                if (item.ProviderName == null)
                    return null;

                CompletionProvider? provider = null;
                using var _ = PooledDelegates.GetPooledFunction(static (p, n) => p.Name == n, item.ProviderName, out Func<CompletionProvider, bool> isNameMatchingProviderPredicate);

                lock (_gate)
                {
                    if (!_nameToProvider.TryGetValue(item.ProviderName, out provider))
                    {
                        provider = GetImportedAndBuiltInProviders(roles: ImmutableHashSet<string>.Empty).FirstOrDefault(isNameMatchingProviderPredicate);
                        _nameToProvider.Add(item.ProviderName, provider);
                    }
                }

                return provider ?? GetProjectCompletionProviders(project).FirstOrDefault(isNameMatchingProviderPredicate);
            }

            public ConcatImmutableArray<CompletionProvider> GetFilteredProviders(
                Project? project, ImmutableHashSet<string>? roles, CompletionTrigger trigger, in CompletionOptions options)
            {
                var allCompletionProviders = FilterProviders(GetImportedAndBuiltInProviders(roles), trigger, options);
                var projectCompletionProviders = FilterProviders(GetProjectCompletionProviders(project), trigger, options);
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

                return ImmutableArray<CompletionProvider>.Empty;
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
                    => ImmutableArray.Create(exportAttribute.Language);

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

            internal readonly struct TestAccessor
            {
                private readonly ProviderManager _providerManager;

                public TestAccessor(ProviderManager providerManager)
                {
                    _providerManager = providerManager;
                }

                public ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string> roles, Project? project)
                {
                    using var _ = ArrayBuilder<CompletionProvider>.GetInstance(out var providers);
                    providers.AddRange(_providerManager.GetImportedAndBuiltInProviders(roles));
                    providers.AddRange(GetProjectCompletionProviders(project));
                    return providers.ToImmutable();
                }
            }
        }
    }
}
