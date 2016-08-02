// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide compilation from given solution checksum 
    ///
    /// TODO: change this to workspace service
    /// </summary>
    internal class CompilationService
    {
        public async Task<Compilation> GetCompilationAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
        {
            var solution = await RoslynServices.SolutionService.GetSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // TODO: need to figure out how to deal with exceptions in service hub
            return await solution.GetProject(projectId).GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
