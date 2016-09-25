// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Arguments;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService
    {
        public Task SymbolSearch_UpdateContinuouslyAsync(
            string sourceName, string localSettingsDirectory, byte[] solutionChecksum)
        {
            var logService = new LogService(this);
            return RoslynServices.SymbolSearchService.UpdateContinuouslyAsync(
                logService, sourceName, localSettingsDirectory);
        }

        public async Task<SerializablePackageWithTypeResult[]> SymbolSearch_FindPackagesWithType(
            string source, string name, int arity, byte[] solutionChecksum)
        {
            var results = await RoslynServices.SymbolSearchService.FindPackagesWithType(
                source, name, arity, CancellationToken).ConfigureAwait(false);
            var serializedResults = results.Select(SerializablePackageWithTypeResult.Dehydrate).ToArray();
            return serializedResults;
        }

        public async Task<SerializableReferenceAssemblyWithTypeResult[]> SymbolSearch_FindReferenceAssembliesWithType(
            string name, int arity, byte[] solutionChecksum)
        {
            var results = await RoslynServices.SymbolSearchService.FindReferenceAssembliesWithType(
                name, arity, CancellationToken).ConfigureAwait(false);
            var serializedResults = results.Select(SerializableReferenceAssemblyWithTypeResult.Dehydrate).ToArray();
            return serializedResults;
        }

        private class LogService : ISymbolSearchLogService
        {
            private readonly CodeAnalysisService _service;

            public LogService(CodeAnalysisService service)
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