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
using Api = Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;
using Legacy = Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Internal;

[ExportWorkspaceService(typeof(IReferenceCleanupService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioReferenceCleanupService : IReferenceCleanupService
{
    private readonly Legacy.IProjectSystemReferenceCleanupService? _legacyService;
    private readonly Api.IProjectSystemReferenceCleanupService? _unifiedService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioReferenceCleanupService(
        [Import(AllowDefault = true)] Legacy.IProjectSystemReferenceCleanupService? legacyService,
        [Import(AllowDefault = true)] Api.IProjectSystemReferenceCleanupService? unifiedService)
    {
        _legacyService = legacyService;
        _unifiedService = unifiedService;
    }

    public async Task<ImmutableArray<ReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
    {
        if (_unifiedService is not null)
        {
            var references = await _unifiedService.GetProjectReferencesAsync(projectPath, cancellationToken).ConfigureAwait(false);
            return [.. references.Select(static reference => new ReferenceInfo(
                (ReferenceType)reference.ReferenceType,
                reference.ItemSpecification,
                reference.TreatAsUsed,
                [],
                []))];
        }

        if (_legacyService is not null)
        {
            var references = await _legacyService.GetProjectReferencesAsync(projectPath, cancellationToken).ConfigureAwait(false);
            return [.. references.Select(static reference => new ReferenceInfo(
                (ReferenceType)reference.ReferenceType,
                reference.ItemSpecification,
                reference.TreatAsUsed,
                [],
                []))];
        }

        throw new InvalidOperationException("No project-system reference cleanup service is available.");
    }

    public async Task<bool> TryUpdateReferenceAsync(string projectPath, ReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
    {
        if (_unifiedService is not null)
        {
            var service = _unifiedService as Api.IProjectSystemReferenceCleanupService2
                ?? throw new InvalidOperationException("The unified project-system reference cleanup service does not support update operations.");
            var referenceInfo = referenceUpdate.ReferenceInfo;
            var operation = await service.GetUpdateReferenceOperationAsync(
                projectPath,
                new Api.ProjectSystemReferenceUpdate(
                    referenceUpdate.Action switch
                    {
                        UpdateAction.TreatAsUsed => Api.ProjectSystemUpdateAction.SetTreatAsUsed,
                        UpdateAction.TreatAsUnused => Api.ProjectSystemUpdateAction.UnsetTreatAsUsed,
                        UpdateAction.Remove => Api.ProjectSystemUpdateAction.Remove,
                        _ => throw ExceptionUtilities.Unreachable()
                    },
                    new Api.ProjectSystemReferenceInfo(
                        (Api.ProjectSystemReferenceType)referenceInfo.ReferenceType,
                        referenceInfo.ItemSpecification,
                        referenceInfo.TreatAsUsed)),
                cancellationToken).ConfigureAwait(true);
            return operation is not null && await operation.ApplyAsync(cancellationToken).ConfigureAwait(true);
        }

        if (_legacyService is not null)
        {
            var service = _legacyService as Legacy.IProjectSystemReferenceCleanupService2
                ?? throw new InvalidOperationException("The legacy project-system reference cleanup service does not support update operations.");
            var referenceInfo = referenceUpdate.ReferenceInfo;
            var operation = await service.GetUpdateReferenceOperationAsync(
                projectPath,
                new Legacy.ProjectSystemReferenceUpdate(
                    referenceUpdate.Action switch
                    {
                        UpdateAction.TreatAsUsed => Legacy.ProjectSystemUpdateAction.SetTreatAsUsed,
                        UpdateAction.TreatAsUnused => Legacy.ProjectSystemUpdateAction.UnsetTreatAsUsed,
                        UpdateAction.Remove => Legacy.ProjectSystemUpdateAction.Remove,
                        _ => throw ExceptionUtilities.Unreachable()
                    },
                    new Legacy.ProjectSystemReferenceInfo(
                        (Legacy.ProjectSystemReferenceType)referenceInfo.ReferenceType,
                        referenceInfo.ItemSpecification,
                        referenceInfo.TreatAsUsed)),
                cancellationToken).ConfigureAwait(true);
            return operation is not null && await operation.ApplyAsync(cancellationToken).ConfigureAwait(true);
        }

        throw new InvalidOperationException("No project-system reference cleanup service is available.");
    }
}
