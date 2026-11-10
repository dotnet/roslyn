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
using ProjectSystemApi = global::Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Internal;

[ExportWorkspaceService(typeof(IReferenceCleanupService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioReferenceCleanupService : IReferenceCleanupService
{
    private readonly ProjectSystemApi.IProjectSystemReferenceCleanupService? _unifiedService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioReferenceCleanupService(
        [Import(AllowDefault = true)] ProjectSystemApi.IProjectSystemReferenceCleanupService? unifiedService)
        => _unifiedService = unifiedService;

    public async Task<ImmutableArray<ReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
    {
        var service = _unifiedService ?? throw new InvalidOperationException("No project-system reference cleanup service is available.");
        var references = await service.GetProjectReferencesAsync(projectPath, cancellationToken).ConfigureAwait(false);
        return [.. references.Select(static reference => new ReferenceInfo(
            (ReferenceType)reference.ReferenceType,
            reference.ItemSpecification,
            reference.TreatAsUsed,
            [],
            []))];
    }

    public async Task<bool> TryUpdateReferenceAsync(string projectPath, ReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
    {
        var service = _unifiedService ?? throw new InvalidOperationException("No project-system reference cleanup service is available.");
        var referenceInfo = referenceUpdate.ReferenceInfo;
        var operation = await service.GetUpdateReferenceOperationAsync(
            projectPath,
            new ProjectSystemApi.ProjectSystemReferenceUpdate(
                referenceUpdate.Action switch
                {
                    UpdateAction.TreatAsUsed => ProjectSystemApi.ProjectSystemUpdateAction.SetTreatAsUsed,
                    UpdateAction.TreatAsUnused => ProjectSystemApi.ProjectSystemUpdateAction.UnsetTreatAsUsed,
                    UpdateAction.Remove => ProjectSystemApi.ProjectSystemUpdateAction.Remove,
                    _ => throw ExceptionUtilities.Unreachable()
                },
                new ProjectSystemApi.ProjectSystemReferenceInfo(
                    (ProjectSystemApi.ProjectSystemReferenceType)referenceInfo.ReferenceType,
                    referenceInfo.ItemSpecification,
                    referenceInfo.TreatAsUsed)),
            cancellationToken).ConfigureAwait(true);
        return operation is not null && await operation.ApplyAsync(cancellationToken).ConfigureAwait(true);
    }
}
