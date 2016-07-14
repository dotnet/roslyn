// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal class CodeAnalysisService : ServiceHubJsonRpcServiceBase
    {
        public CodeAnalysisService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
        }

        public async Task<string> CalculateDiagnosticsAsync(byte[] checksum, Guid guid, string debugName, string[] analyzerIds)
        {
            // entry point for diagnostic service
            var solutionSnapshotId = await RoslynServices.AssetService.GetAssetAsync<SolutionSnapshotId>(new Checksum(ImmutableArray.Create(checksum))).ConfigureAwait(false);
            var projectId = ProjectId.CreateFromSerialized(guid, debugName);

            var result = await (new DiagnosticComputer()).GetDiagnosticsAsync(solutionSnapshotId, projectId, analyzerIds, CancellationToken).ConfigureAwait(false);

            // just for testing
            return result.AnalysisResult.Count.ToString();
        }
    }
}
