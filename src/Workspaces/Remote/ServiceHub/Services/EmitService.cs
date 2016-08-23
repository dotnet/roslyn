// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.TestImpact.BuildManagement
{
    internal partial class EmitService : ServiceHubServiceBase
    {
        public EmitService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
        }

        public async Task<EmitResult> EmitAsync(
            Guid guid,
            string debugName,
            string outputFilePath,
            string win32ResourcesPath,
            ImmutableArray<ResourceDescription> manifestResources,
            EmitOptions options,
            string runtimeReferencePath,
            byte[] solutionChecksum)
        {
            var projectId = ProjectId.CreateFromSerialized(guid, debugName);
            var solution = await RoslynServices.SolutionService.GetSolutionAsync(new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);
            var project = solution.GetProject(projectId);
            return await project.EmitAsync(
                outputFilePath,
                win32ResourcesPath,
                manifestResources,
                options,
                runtimeReferencePath,
                CancellationToken).ConfigureAwait(false);
        }
    }
}
