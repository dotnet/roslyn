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
    [ExportWorkspaceService(typeof(IReferenceCleanupService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioReferenceCleanupService : IReferenceCleanupService
    {
        private readonly IProjectSystemReferenceCleanupService _projectSystemReferenceUpdateService;
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioReferenceCleanupService(
            IProjectSystemReferenceCleanupService projectSystemReferenceUpdateService,
            VisualStudioWorkspaceImpl workspace)
        {
            _projectSystemReferenceUpdateService = projectSystemReferenceUpdateService;
            _workspace = workspace;
        }

        public string GetTargetFrameworkMoniker(ProjectId projectId)
        {
            if (_workspace.TryGetHierarchy(projectId, out var hierarchy) &&
                hierarchy.TryGetTargetFrameworkMoniker((uint)VSConstants.VSITEMID.Root, out var targetFrameworkMoniker))
            {
                return targetFrameworkMoniker ?? string.Empty;
            }

            return string.Empty;
        }

        public Task<string> GetProjectAssetsFilePathAsync(string projectPath, CancellationToken cancellationToken)
        {
#if false // PROTOTYPE: Temporary disabling code causing a build break
            return _projectSystemReferenceUpdateService.GetProjectAssetsFilePathAsync(projectPath, cancellationToken);
#else
            return Task<string>.FromResult("");
#endif
        }

        public async Task<ImmutableArray<ReferenceInfo>> GetProjectReferencesAsync(string projectPath, string targetFramework, CancellationToken cancellationToken)
        {
#if false // PROTOTYPE: Temporary disabling code causing a build break
            var projectSystemReferences = await _projectSystemReferenceUpdateService.GetProjectReferencesAsync(projectPath, targetFramework, cancellationToken).ConfigureAwait(false);
            return projectSystemReferences.Select(reference => reference.ToReferenceInfo()).ToImmutableArray();
#else
            return await Task<ImmutableArray<ReferenceInfo>>.FromResult(ImmutableArray<ReferenceInfo>.Empty).ConfigureAwait(false);
#endif
        }

        public Task<bool> TryUpdateReferenceAsync(string projectPath, string targetFramework, ReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
#if false // PROTOTYPE: Temporary disabling code causing a build break
            return _projectSystemReferenceUpdateService.TryUpdateReferenceAsync(projectPath, targetFramework, referenceUpdate.ToProjectSystemReferenceUpdate(), cancellationToken);
#else
            return Task<bool>.FromResult(false);
#endif
        }
    }
}
