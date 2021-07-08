// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [ExportWorkspaceServiceFactory(typeof(IOptionService), ServiceLayer.Host), Shared]
    internal class TestOptionsServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IWorkspaceThreadingService? _workspaceThreadingService;
        private readonly ImmutableArray<Lazy<IOptionProvider, LanguageMetadata>> _providers;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestOptionsServiceFactory(
            [Import(AllowDefault = true)] IWorkspaceThreadingService? workspaceThreadingService,
            [ImportMany] IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders)
        {
            _workspaceThreadingService = workspaceThreadingService;
            _providers = optionProviders.ToImmutableArray();
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // give out new option service per workspace
            return new OptionServiceFactory.OptionService(
                new GlobalOptionService(_workspaceThreadingService, _providers, SpecializedCollections.EmptyEnumerable<Lazy<IOptionPersisterProvider>>()),
                workspaceServices);
        }
    }
}
