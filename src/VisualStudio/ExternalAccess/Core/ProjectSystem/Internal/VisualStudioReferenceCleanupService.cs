// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Internal;

[ExportWorkspaceService(typeof(IReferenceCleanupService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioReferenceCleanupService : IReferenceCleanupService
{
    private readonly IProjectSystemReferenceCleanupService _unifiedService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioReferenceCleanupService(
        IProjectSystemReferenceCleanupService unifiedService)
        => _unifiedService = unifiedService;

    public async Task<ImmutableArray<ReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
    {
        var references = await _unifiedService.GetProjectReferencesAsync(projectPath, cancellationToken).ConfigureAwait(false);
        return [.. references.Select(static reference => new ReferenceInfo(
            (ReferenceType)reference.ReferenceType,
            reference.ItemSpecification,
            reference.TreatAsUsed,
            [],
            []))];
    }

    public async Task<bool> TryUpdateReferenceAsync(string projectPath, ReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
    {
        var referenceInfo = referenceUpdate.ReferenceInfo;
        var operation = await _unifiedService.GetUpdateReferenceOperationAsync(
            projectPath,
            new ProjectSystemReferenceUpdate(
                referenceUpdate.Action switch
                {
                    UpdateAction.TreatAsUsed => ProjectSystemUpdateAction.SetTreatAsUsed,
                    UpdateAction.TreatAsUnused => ProjectSystemUpdateAction.UnsetTreatAsUsed,
                    UpdateAction.Remove => ProjectSystemUpdateAction.Remove,
                    _ => throw ExceptionUtilities.Unreachable()
                },
                new ProjectSystemReferenceInfo(
                    (ProjectSystemReferenceType)referenceInfo.ReferenceType,
                    referenceInfo.ItemSpecification,
                    referenceInfo.TreatAsUsed)),
            cancellationToken).ConfigureAwait(true);
        return operation is not null && await operation.ApplyAsync(cancellationToken).ConfigureAwait(true);
    }
}
