// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteSymbolSearchUpdateEngine : ServiceHubServiceBase, IRemoteSymbolSearchUpdateEngine
    {
        private readonly SymbolSearchUpdateEngine _updateEngine;

        public RemoteSymbolSearchUpdateEngine(Stream stream, IServiceProvider serviceProvider) 
            : base(stream, serviceProvider)
        {
            _updateEngine = new SymbolSearchUpdateEngine(
                new LogService(this), updateCancellationToken: this.CancellationToken);
        }

        public Task UpdateContinuouslyAsync(
            string sourceName, string localSettingsDirectory, byte[] solutionChecksum)
        {
            return _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory);
        }

        public async Task<SerializablePackageWithTypeResult[]> FindPackagesWithTypeAsync(
            string source, string name, int arity, byte[] solutionChecksum)
        {
            var results = await _updateEngine.FindPackagesWithTypeAsync(
                source, name, arity).ConfigureAwait(false);
            var serializedResults = results.Select(SerializablePackageWithTypeResult.Dehydrate).ToArray();
            return serializedResults;
        }

        public async Task<SerializableReferenceAssemblyWithTypeResult[]> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, byte[] solutionChecksum)
        {
            var results = await _updateEngine.FindReferenceAssembliesWithTypeAsync(
                name, arity).ConfigureAwait(false);
            var serializedResults = results.Select(SerializableReferenceAssemblyWithTypeResult.Dehydrate).ToArray();
            return serializedResults;
        }

        private class LogService : ISymbolSearchLogService
        {
            private readonly RemoteSymbolSearchUpdateEngine _service;

            public LogService(RemoteSymbolSearchUpdateEngine service)
            {
                _service = service;
            }

            public Task LogExceptionAsync(string exception, string text)
                => _service.Rpc.InvokeAsync(nameof(LogExceptionAsync), exception, text);

            public Task LogInfoAsync(string text)
                => _service.Rpc.InvokeAsync(nameof(LogInfoAsync), text);
        }
    }
}