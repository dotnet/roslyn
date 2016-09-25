// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Services
{
    internal class RemoteSymbolSearchService
    {
        private readonly object _gate = new object();

        private SymbolSearchUpdateEngine _updateEngine;

        private SymbolSearchUpdateEngine GetUpdateEngine()
        {
            lock (_gate)
            {
                return _updateEngine;
            }
        }

        private SymbolSearchUpdateEngine GetUpdateEngine(ISymbolSearchLogService logService)
        {
            lock (_gate)
            {
                if (_updateEngine == null)
                {
                    _updateEngine = new SymbolSearchUpdateEngine(logService);
                }

                return _updateEngine;
            }
        }

        public Task UpdateContinuouslyAsync(
            ISymbolSearchLogService logService, 
            string source,
            string localSettingsDirectory)
        {
            var updateEngine = GetUpdateEngine(logService);
            return updateEngine.UpdateContinuouslyAsync(source, localSettingsDirectory);
        }

        public Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithType(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
            var updateEngine = GetUpdateEngine();
            if (updateEngine == null)
            {
                return SpecializedTasks.EmptyImmutableArray<PackageWithTypeResult>();
            }

            return updateEngine.FindPackagesWithTypeAsync(source, name, arity, cancellationToken);
        }

        public Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithType(
            string name, int arity, CancellationToken cancellationToken)
        {
            var updateEngine = GetUpdateEngine();
            if (updateEngine == null)
            {
                return SpecializedTasks.EmptyImmutableArray<ReferenceAssemblyWithTypeResult>();
            }

            return updateEngine.FindReferenceAssembliesWithTypeAsync(name, arity, cancellationToken);
        }
    }
}