// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed partial class RemoteSemanticTokensService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteSemanticTokensService
{
    internal sealed class Factory : FactoryBase<IRemoteSemanticTokensService>
    {
        protected override IRemoteSemanticTokensService CreateService(in ServiceArgs args)
            => new RemoteSemanticTokensService(in args);
    }

    private readonly IRazorSemanticTokensInfoService _semanticTokensInfoService = args.ExportProvider.GetExportedValue<IRazorSemanticTokensInfoService>();
    private readonly IClientSettingsManager _clientSettingsManager = args.ExportProvider.GetExportedValue<IClientSettingsManager>();

    public ValueTask<int[]?> GetSemanticTokensDataAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        LinePositionSpan span,
        Guid correlationId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetSemanticTokensDataAsync(context, span, correlationId, cancellationToken),
            cancellationToken);

    private async ValueTask<int[]?> GetSemanticTokensDataAsync(
        RemoteDocumentContext context,
        LinePositionSpan span,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        return await _semanticTokensInfoService
            .GetSemanticTokensAsync(context, span, _clientSettingsManager.GetClientSettings().AdvancedSettings.ColorBackground, correlationId, cancellationToken)
            .ConfigureAwait(false);
    }
}
