// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public abstract partial class CompletionServiceWithProviders
    {
        private sealed class ProviderManager : IEqualityComparer<ImmutableHashSet<string>>
        {
            private readonly object _gate = new();
            private readonly Dictionary<string, CompletionProvider?> _nameToProvider = new();
            private readonly Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>> _rolesToProviders;

            private readonly Func<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>> _createRoleProviders;
            private readonly Func<string, CompletionProvider?> _getProviderByName;

            private IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>>? _lazyImportedProviders;
            private readonly CompletionServiceWithProviders _service;

            public ProviderManager(CompletionServiceWithProviders service)
            {
                _service = service;
                _rolesToProviders = new Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>>(this);
                _createRoleProviders = CreateRoleProviders;
                _getProviderByName = GetProviderByName;
            }

            public IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> GetImportedProviders()
            {
                if (_lazyImportedProviders == null)
                {
                    var language = _service.Language;
                    var mefExporter = (IMefHostExportProvider)_service._workspace.Services.HostServices;

                    var providers = ExtensionOrderer.Order(
                            mefExporter.GetExports<CompletionProvider, CompletionProviderMetadata>()
                            .Where(lz => lz.Metadata.Language == language)
                            ).ToList();

                    Interlocked.CompareExchange(ref _lazyImportedProviders, providers, null);
                }

                return _lazyImportedProviders;
            }

            public ImmutableArray<CompletionProvider> GetAllProviders(ImmutableHashSet<string> roles)
            {
                var imported = GetImportedProviders()
                    .Where(lz => lz.Metadata.Roles == null || lz.Metadata.Roles.Length == 0 || roles.Overlaps(lz.Metadata.Roles))
                    .Select(lz => lz.Value);

#pragma warning disable 0618
                // We need to keep supporting built-in providers for a while longer since this is a public API.
                // https://github.com/dotnet/roslyn/issues/42367
                var builtin = _service.GetBuiltInProviders();
#pragma warning restore 0618

                return imported.Concat(builtin).ToImmutableArray();
            }

            public ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string>? roles)
            {
                roles ??= ImmutableHashSet<string>.Empty;

                lock (_gate)
                {
                    return _rolesToProviders.GetOrAdd(roles, _createRoleProviders);
                }
            }

            public CompletionProvider? GetProvider(CompletionItem item)
            {
                CompletionProvider? provider = null;

                if (item.ProviderName != null)
                {
                    lock (_gate)
                    {
                        provider = _nameToProvider.GetOrAdd(item.ProviderName, _getProviderByName);
                    }
                }

                return provider;
            }

            public ConcatImmutableArray<CompletionProvider> GetFilteredProviders(
                Project? project, ImmutableHashSet<string>? roles, CompletionTrigger trigger, in CompletionOptions options)
            {
                // We need to call `GetProviders` from the service since it could be overridden by its subclasses.
                var allCompletionProviders = FilterProviders(_service.GetProviders(roles, trigger), trigger, options);
                var projectCompletionProviders = FilterProviders(GetProjectCompletionProviders(project), trigger, options);
                return allCompletionProviders.ConcatFast(projectCompletionProviders);
            }

            public static ImmutableArray<CompletionProvider> GetProjectCompletionProviders(Project? project)
            {
                if (project?.Solution.Workspace.Kind == WorkspaceKind.Interactive)
                {
                    // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict completions in Interactive
                    return ImmutableArray<CompletionProvider>.Empty;
                }

                return ProjectCompletionProvider.GetExtensions(project);
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

            private CompletionProvider? GetProviderByName(string providerName)
            {
                var providers = GetAllProviders(roles: ImmutableHashSet<string>.Empty);
                return providers.FirstOrDefault(p => p.Name == providerName);
            }

            private ImmutableArray<CompletionProvider> CreateRoleProviders(ImmutableHashSet<string> roles)
            {
                var providers = GetAllProviders(roles);

                foreach (var provider in providers)
                {
                    _nameToProvider[provider.Name] = provider;
                }

                return providers;
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
                protected override bool SupportsLanguage(ExportCompletionProviderAttribute exportAttribute, string language)
                {
                    return exportAttribute.Language == null
                        || exportAttribute.Language.Length == 0
                        || exportAttribute.Language.Contains(language);
                }

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
        }
    }
}
