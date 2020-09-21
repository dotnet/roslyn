// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences
{
    /// <summary>
    /// This service forwards Reference requests from the feature layer to the ProjectSystem.
    /// </summary>
    [ExportWorkspaceService(typeof(IReferenceUpdateService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioReferenceUpdateService : IReferenceUpdateService
    {
        private readonly IProjectSystemReferenceUpdateService _projectSystemReferenceUpdateService;
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioReferenceUpdateService(
            IProjectSystemReferenceUpdateService projectSystemReferenceUpdateService,
            VisualStudioWorkspaceImpl workspace)
        {
            _projectSystemReferenceUpdateService = projectSystemReferenceUpdateService;
            _workspace = workspace;
        }

        public string GetTargetFramworkMoniker(ProjectId projectId)
        {
            if (_workspace.TryGetHierarchy(projectId, out var hierarchy) &&
                hierarchy.TryGetTargetFrameworkMoniker((uint)VSConstants.VSITEMID.Root, out var targetFrameworkMoniker))
            {
                return targetFrameworkMoniker ?? string.Empty;
            }

            return string.Empty;
        }

        public Task<string> GetProjectAssetsFilePathAsync(string projectPath, string targetFramework, CancellationToken cancellationToken)
        {
            return _projectSystemReferenceUpdateService.GetProjectAssetsFilePathAsync(projectPath, targetFramework, cancellationToken);
        }

        public async Task<ImmutableArray<Reference>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
        {
            var projectSystemReferences = await _projectSystemReferenceUpdateService.GetProjectReferencesAsync(projectPath, cancellationToken).ConfigureAwait(false);
            return projectSystemReferences.Select(reference => reference.ToReference()).ToImmutableArray();
        }

        public Task<bool> UpdateReferencesAsync(string projectPath, string targetFramework, ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
        {
            var projectSystemReferenceUpdates = referenceUpdates.Select(update => update.ToProjectSystemReferenceUpdate()).ToImmutableArray();
            return _projectSystemReferenceUpdateService.UpdateReferencesAsync(projectPath, targetFramework, projectSystemReferenceUpdates, cancellationToken);
        }
    }
}
