// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    public abstract partial class CompletionServiceWithProviders
    {
        private class ProviderSource : IEqualityComparer<ImmutableHashSet<string>>
        {
            private readonly CompletionServiceWithProviders _service;
            private IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>>? _importedProviders;

            private readonly object _gate = new();
            private readonly Dictionary<string, CompletionProvider?> _nameToProvider = new();
            private readonly Dictionary<ImmutableHashSet<string>, Task<ImmutableArray<CompletionProvider>>> _rolesToProviders;

            public ProviderSource(CompletionServiceWithProviders service)
            {
                _service = service;
                _rolesToProviders = new(this);
            }

            public ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string> roles, bool waitUntilAvaialble = false)
            {
                Task<ImmutableArray<CompletionProvider>>? createProviderTask;

                lock (_gate)
                {
                    if (!_rolesToProviders.TryGetValue(roles, out createProviderTask))
                    {
                        createProviderTask = GetAllProvidersAsync(roles);
                        _rolesToProviders[roles] = createProviderTask;
                    }
                }

                return createProviderTask.IsCompleted || waitUntilAvaialble
                    ? createProviderTask.Result
                    : ImmutableArray<CompletionProvider>.Empty;
            }

            public CompletionProvider? GetProviderByName(string name)
            {
                lock (_gate)
                {
                    _nameToProvider.TryGetValue(name, out var provider);
                    return provider;
                }
            }

            private async Task<ImmutableArray<CompletionProvider>> GetAllProvidersAsync(ImmutableHashSet<string> roles)
            {
                await Task.Yield();

                var imported = GetImportedProviders()
                    .Where(lz => lz.Metadata.Roles == null || lz.Metadata.Roles.Length == 0 || roles.Overlaps(lz.Metadata.Roles))
                    .Select(lz => lz.Value);

#pragma warning disable 0618
                // We need to keep supporting built-in providers for a while longer since this is a public API.
                // https://github.com/dotnet/roslyn/issues/42367
                var builtin = _service.GetBuiltInProviders();
#pragma warning restore 0618

                var allProviders = imported.Concat(builtin).ToImmutableArray();

                lock (_gate)
                {
                    foreach (var provider in allProviders)
                    {
                        _nameToProvider[provider.Name] = provider;
                    }
                }

                return allProviders;
            }

            private IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> GetImportedProviders()
            {
                if (_importedProviders == null)
                {
                    var language = _service.Language;
                    var mefExporter = (IMefHostExportProvider)_service._workspace.Services.HostServices;

                    var providers = ExtensionOrderer.Order(
                            mefExporter.GetExports<CompletionProvider, CompletionProviderMetadata>()
                            .Where(lz => lz.Metadata.Language == language)
                            ).ToList();

                    Interlocked.CompareExchange(ref _importedProviders, providers, null);
                }

                return _importedProviders;
            }

            bool IEqualityComparer<ImmutableHashSet<string>>.Equals(ImmutableHashSet<string>? x, ImmutableHashSet<string>? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                if (x.Count != y.Count)
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

            int IEqualityComparer<ImmutableHashSet<string>>.GetHashCode(ImmutableHashSet<string> obj)
            {
                var hash = 0;
                foreach (var o in obj)
                {
                    hash += o.GetHashCode();
                }

                return hash;
            }
        }
    }
}
