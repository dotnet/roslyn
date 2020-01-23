// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide compilation from given solution checksum 
    ///
    /// TODO: change this to workspace service
    /// </summary>
    internal class CompilationService
    {
        private readonly SolutionService _solutionService;

        public CompilationService(SolutionService solutionService)
        {
            _solutionService = solutionService;
        }

        public async Task<Compilation> GetCompilationAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CompilationService_GetCompilationAsync, GetLogInfo, solutionChecksum, projectId, cancellationToken))
            {
                var solution = await _solutionService.GetSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                return await solution.GetProject(projectId).GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static string GetLogInfo(Checksum checksum, ProjectId projectId)
        {
            return $"{checksum.ToString()} - {projectId.ToString()}";
        }
    }
}
