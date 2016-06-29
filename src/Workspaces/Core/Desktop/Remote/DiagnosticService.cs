// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class DiagnosticService : ServiceHubJsonRpcServiceBase
    {
        public DiagnosticService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
        }

        public async Task CalculateAsync(byte[] checksum, Guid guid, string debugName)
        {
            // TODO: figure out how to deal with cancellation
            var manager = RoslynServiceHubServices.Asset;
            var solutionSnapshotId = await manager.GetAssetAsync<SolutionSnapshotId>(new Checksum(ImmutableArray.Create(checksum))).ConfigureAwait(false);

            var compilationService = RoslynServiceHubServices.Compilation;
            var projectId = ProjectId.CreateFromSerialized(guid, debugName);

            var compilation = await compilationService.GetCompilationAsync(solutionSnapshotId, projectId, Token).ConfigureAwait(false);

            var diagnostics = compilation.GetDiagnostics(Token);
            Logger.TraceInformation(string.Join("|", diagnostics.Select(d => d.GetMessage())));
        }
    }
}
