// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
