// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

[ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared]
internal sealed partial class CSharpCopilotCodeAnalysisService : AbstractCopilotCodeAnalysisService
{
    private readonly Lazy<IExternalCSharpCopilotCodeAnalysisService?> _lazyExternalCopilotService;

    // Check for UIContext first to avoid loading copilot unnecessarily
    public override bool IsCopilotAvailable => IsCopilotSignedIn && _lazyExternalCopilotService.Value is not null;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotCodeAnalysisService(
        [Import(AllowDefault = true)] Lazy<IExternalCSharpCopilotCodeAnalysisService?> externalCopilotService,
        IDiagnosticsRefresher diagnosticsRefresher,
        SVsServiceProvider serviceProvider,
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer
        ) : base()
    {
        _lazyExternalCopilotService = externalCopilotService;
    }

    public override async Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
    {
        if (!IsCopilotAvailable)
            return;

        if (oldDocument.GetLanguageService<ICopilotOptionsService>() is not { } service)
            return;

        if (await service.IsRefineOptionEnabledAsync().ConfigureAwait(false))
            await _lazyExternalCopilotService.Value.StartRefinementSessionAsync(oldDocument, newDocument, primaryDiagnostic, cancellationToken).ConfigureAwait(false);
    }

    public override Task<string> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
    {
        if (!IsCopilotAvailable)
            return Task.FromResult(string.Empty);

        return _lazyExternalCopilotService.Value.GetOnTheFlyDocsAsync(symbolSignature, declarationCode, language, cancellationToken);
    }

    public override Task<bool> IsAnyExclusionAsync(CancellationToken cancellationToken)
    {
        if (!IsCopilotAvailable)
            return Task.FromResult(false);

        return _lazyExternalCopilotService.Value.IsAnyExclusionAsync(cancellationToken);
    }
}
