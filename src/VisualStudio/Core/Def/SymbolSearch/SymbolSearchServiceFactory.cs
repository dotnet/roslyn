// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    [ExportWorkspaceServiceFactory(typeof(ISymbolSearchService), WorkspaceKind.Host), Shared]
    internal class SymbolSearchServiceFactory : IWorkspaceServiceFactory
    {
        private readonly VSShell.SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public SymbolSearchServiceFactory(
            VSShell.SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var options = workspaceServices.Workspace.Options;
            if (options.GetOption(ServiceComponentOnOffOptions.SymbolSearch))
            {
                // Only support package search in vs workspace.
                if (workspaceServices.Workspace is VisualStudioWorkspace)
                {
                    return new SymbolSearchService(
                        _serviceProvider, workspaceServices.Workspace,
                        workspaceServices.GetService<IPackageInstallerService>());
                }
            }

            return new NullSymbolSearchService();
        }

        private class NullSymbolSearchService : ISymbolSearchService
        {
            public IEnumerable<PackageWithTypeResult> FindPackagesWithType(
                string source, string name, int arity, CancellationToken cancellationToken)
            {
                return SpecializedCollections.EmptyEnumerable<PackageWithTypeResult>();
            }

            public IEnumerable<ReferenceAssemblyWithTypeResult> FindReferenceAssembliesWithType(
                string name, int arity, CancellationToken cancellationToken)
            {
                return SpecializedCollections.EmptyEnumerable<ReferenceAssemblyWithTypeResult>();
            }
        }
    }
}
