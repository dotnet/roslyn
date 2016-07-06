// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: currently, service hub provide no other way to share services between user service hub services.
    //       only way to do so is using static type
    // TODO: this whole thing should be refactored/improved
    internal class CompilationService
    {
        public async Task<Compilation> GetCompilationAsync(SolutionSnapshotId id, ProjectId projectId, CancellationToken cancellationToken)
        {
            var solution = await RoslynServices.SolutionService.GetSolutionAsync(id, cancellationToken).ConfigureAwait(false);

            // TODO: need to figure out how to deal with exceptions in service hub
            return await solution.GetProject(projectId).GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
