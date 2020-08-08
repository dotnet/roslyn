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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    /// <summary>
    /// <see cref="IOptionService"/> factory that allows creating multiple test workspaces with shared <see cref="IGlobalOptionService"/>.
    /// This mimics the real product scenarios where all workspaces share the same global options service.
    /// Note that majority of unit tests use <see cref="TestOptionsServiceFactory"/> instead of this factory to ensure options isolation between each test.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IOptionService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal class TestOptionsServiceWithSharedGlobalOptionsServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestOptionsServiceWithSharedGlobalOptionsServiceFactory(
            [ImportMany] IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders)
        {
            _globalOptionService = new GlobalOptionService(optionProviders.ToImmutableArray(), SpecializedCollections.EmptyEnumerable<Lazy<IOptionPersister>>());
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // give out new option service per workspace, but share the global option service
            return new OptionServiceFactory.OptionService(_globalOptionService, workspaceServices);
        }
    }
}
