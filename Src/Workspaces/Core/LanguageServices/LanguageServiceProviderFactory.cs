// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal partial class LanguageServiceProviderFactory : ILanguageServiceProviderFactory
    {
        private readonly IWorkspaceServiceProvider workspaceServices;
        private readonly ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>> services;
        private readonly ConcurrentDictionary<string, LanguageServiceProvider> providerMap = new ConcurrentDictionary<string, LanguageServiceProvider>();

        public LanguageServiceProviderFactory(
            IWorkspaceServiceProvider workspaceServices,
            ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>> services)
        {
            this.workspaceServices = workspaceServices;
            this.services = services;
            this.constructLanguageServiceProvider = this.ConstructLanguageServiceProvider;
        }

        public IWorkspaceServiceProvider WorkspaceServices
        {
            get { return this.workspaceServices; }
        }

        private readonly Func<string, LanguageServiceProvider> constructLanguageServiceProvider;

        private LanguageServiceProvider ConstructLanguageServiceProvider(string language)
        {
            var unboundServicesMap = GetServiceMap(language, this.workspaceServices.Kind, this.services);
            return new LanguageServiceProvider(this, language, unboundServicesMap);
        }

        private static ImmutableDictionary<string, Func<ILanguageServiceProvider, ILanguageService>> GetServiceMap(
            string language,
            string workspaceKind,
            IEnumerable<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>> services)
        {
            return ImmutableDictionary.CreateRange<string, Func<ILanguageServiceProvider, ILanguageService>>(
                        services.Where(ls => ls.Key.Language == language)
                                .ToLookup(ls => ls.Key.ServiceTypeAssemblyQualifiedName)
                                .Select(grp =>
            {
                var best = PickBestService(workspaceKind, grp);
                return new KeyValuePair<string, Func<ILanguageServiceProvider, ILanguageService>>(best.Key.ServiceTypeAssemblyQualifiedName, best.Value);
            })
                                .Where(kvp => kvp.Value != null));
        }

        private static KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>> PickBestService(string workspaceKind, IEnumerable<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>> services)
        {
            var kind = workspaceKind == WorkspaceKind.Any ? WorkspaceKind.Host : workspaceKind;

            // exact workspace kind match wins
            var kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == kind);
            if (kvp.Value != null)
            {
                return kvp;
            }

            // host services override editor or default
            kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == WorkspaceKind.Host);
            if (kvp.Value != null)
            {
                return kvp;
            }

            // editor services override default
            kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == WorkspaceKind.Editor);
            if (kvp.Value != null)
            {
                return kvp;
            }

            // services marked as any are default
            kvp = services.SingleOrDefault(s => s.Key.WorkspaceKind == WorkspaceKind.Any);
            if (kvp.Value != null)
            {
                return kvp;
            }

            // any service is okay 
            return services.SingleOrDefault();
        }

        private ImmutableList<string> lazySupportedLanguages;

        private ImmutableList<string> GetSupportedLanguages()
        {
            if (this.lazySupportedLanguages == null)
            {
                var list = this.services.Select(s => s.Key.Language).Distinct().ToImmutableList();
                System.Threading.Interlocked.CompareExchange(ref this.lazySupportedLanguages, list, null);
            }

            return this.lazySupportedLanguages;
        }

        public IEnumerable<string> SupportedLanguages
        {
            get { return this.GetSupportedLanguages(); }
        }

        public bool IsSupported(string language)
        {
            return this.GetSupportedLanguages().Contains(language);
        }

        public ILanguageServiceProvider GetLanguageServiceProvider(string language)
        {
            var provider = providerMap.GetOrAdd(language, constructLanguageServiceProvider);

            if (!provider.HasServices)
            {
                throw new NotSupportedException(string.Format(WorkspacesResources.UnsupportedLanguage, language));
            }

            return provider;
        }

        public IEnumerable<T> GetServices<T>() where T : ILanguageService
        {
            return this.SupportedLanguages.Select(lang => this.GetLanguageServiceProvider(lang).GetService<T>()).Where(s => s != null);
        }
    }
}