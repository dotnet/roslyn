// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchUpdateEngine : IWorkspaceService
    {
        IEnumerable<PackageWithTypeResult> FindPackagesWithType(string source, string name, int arity, CancellationToken cancellationToken);
        IEnumerable<ReferenceAssemblyWithTypeResult> FindReferenceAssembliesWithType(string name, int arity, CancellationToken cancellationToken);

        void StopUpdates();
        Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory);
    }

    [ExportWorkspaceServiceFactory(typeof(ISymbolSearchUpdateEngine)), Shared]
    internal class DefaultSymbolSearchUpdateEngineFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SymbolSearchUpdateEngine(workspaceServices);
        }
    }
}