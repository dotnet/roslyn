// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    /// <summary>
    /// <see cref="IOptionService"/> factory that allows creating multiple test workspaces with shared <see cref="IGlobalOptionService"/>.
    /// This mimics the real product scenarios where all workspaces share the same global options service.
    /// Note that majority of unit tests use <see cref="TestOptionsServiceFactory"/> instead of this factory to ensure options isolation between each test.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IOptionService), TestWorkspaceName.NameWithSharedGlobalOptions), Shared]
    internal class TestOptionsServiceWithSharedGlobalOptionsServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
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
