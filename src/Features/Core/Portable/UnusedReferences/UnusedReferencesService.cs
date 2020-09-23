// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    [ExportWorkspaceService(typeof(IUnusedReferencesService)), Shared]
    internal class UnusedReferencesService : IUnusedReferencesService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnusedReferencesService()
        {
        }

        public async Task<ImmutableArray<Reference>> GetUnusedReferencesAsync(Project project, CancellationToken cancellationToken)
        {
            var workspace = project.Solution.Workspace;
            var referenceUpdateService = workspace.Services.GetService<IReferenceCleanupService>();
            if (referenceUpdateService is null)
            {
                return ImmutableArray<Reference>.Empty;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
            {
                return ImmutableArray<Reference>.Empty;
            }

            var usedReferences = compilation.GetUsedAssemblyReferences(cancellationToken);
            var unusedReferences = compilation.References.Except(usedReferences).ToImmutableArray();

            var targetFrameworkMoniker = referenceUpdateService.GetTargetFramworkMoniker(project.Id);
            var projectAssetsFilePath = await referenceUpdateService.GetProjectAssetsFilePathAsync(project.FilePath, targetFrameworkMoniker, cancellationToken).ConfigureAwait(false);

            var projectAssetsText = File.ReadAllText(projectAssetsFilePath);

            return ImmutableArray<Reference>.Empty;
        }

        public Task<Project> UpdateReferencesAsync(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
