// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences
{
    /// <summary>
    /// This service forwards Reference requests from the feature layer to the ProjectSystem.
    /// </summary>
    [ExportWorkspaceService(typeof(IReferenceCleanupService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioReferenceCleanupService : IReferenceCleanupService
    {
        private readonly IProjectSystemReferenceCleanupService2 _projectSystemReferenceUpdateService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioReferenceCleanupService(IProjectSystemReferenceCleanupService projectSystemReferenceUpdateService)
        {
            _projectSystemReferenceUpdateService = (IProjectSystemReferenceCleanupService2)projectSystemReferenceUpdateService;
        }

        public async Task<ImmutableArray<ReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
        {
            var projectSystemReferences = await _projectSystemReferenceUpdateService.GetProjectReferencesAsync(projectPath, cancellationToken).ConfigureAwait(false);
            return projectSystemReferences.Select(reference => reference.ToReferenceInfo()).ToImmutableArray();
        }

        public async Task<bool> TryUpdateReferenceAsync(string projectPath, ReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            var operation = await _projectSystemReferenceUpdateService.GetUpdateReferenceOperationAsync(projectPath, referenceUpdate.ToProjectSystemReferenceUpdate(), cancellationToken).ConfigureAwait(true);
            if (operation is null)
                return false;

            return await operation.ApplyAsync(cancellationToken).ConfigureAwait(true);
        }
    }
}
