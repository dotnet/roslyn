// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    [ExportWorkspaceServiceFactory(typeof(IOptionService), WorkspaceKind.RemoteTemporaryWorkspace), Shared]
    internal class TemporaryWorkspaceOptionsServiceFactory : IWorkspaceServiceFactory
    {
        private readonly ImmutableArray<Lazy<IOptionProvider>> _providers;
        private readonly ImmutableArray<IDocumentOptionsProviderFactory> _documentOptionsProviderFactories;

        [ImportingConstructor]
        public TemporaryWorkspaceOptionsServiceFactory(
            [ImportMany] IEnumerable<Lazy<IOptionProvider>> optionProviders,
            [ImportMany] IEnumerable<IDocumentOptionsProviderFactory> documentOptionsProviderFactories)
        {
            _providers = optionProviders.ToImmutableArray();
            _documentOptionsProviderFactories = documentOptionsProviderFactories.ToImmutableArray();
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // give out new option service per workspace
            var service = new OptionServiceFactory.OptionService(
                new GlobalOptionService(_providers, SpecializedCollections.EmptyEnumerable<Lazy<IOptionPersister>>()),
                workspaceServices);

            foreach (var factory in _documentOptionsProviderFactories)
            {
                service.RegisterDocumentOptionsProvider(factory.Create(workspaceServices.Workspace));
            }

            return service;
        }
    }
}
