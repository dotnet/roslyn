// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

[ExportLanguageServiceFactory(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCopilotCodeAnalysisServiceFactory : ILanguageServiceFactory
{
    private readonly Lazy<IExternalCSharpCopilotCodeAnalysisService?> _lazyExternalCopilotService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotCodeAnalysisServiceFactory([Import(AllowDefault = true)] Lazy<IExternalCSharpCopilotCodeAnalysisService?> externalCopilotService)
    {
        _lazyExternalCopilotService = externalCopilotService;
    }

    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        return new CSharpCopilotCodeAnalysisService(_lazyExternalCopilotService, languageServices.GetRequiredService<ICopilotOptionsService>());
    }

    private sealed class CSharpCopilotCodeAnalysisService : ICopilotCodeAnalysisService
    {
        private readonly Lazy<IExternalCSharpCopilotCodeAnalysisService?> _lazyExternalCopilotService;
        private readonly ICopilotOptionsService _copilotOptionsService;

        public CSharpCopilotCodeAnalysisService(
            Lazy<IExternalCSharpCopilotCodeAnalysisService?> externalCopilotService,
            ICopilotOptionsService copilotOptionsService)
        {
            _lazyExternalCopilotService = externalCopilotService;
            _copilotOptionsService = copilotOptionsService;
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            // Check for UIContext first to avoid loading copilot unnecessarily
            return _copilotOptionsService.IsCopilotLoadedAndSignedIn()
                && _lazyExternalCopilotService.Value is { } service
                && await service.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
        {
            if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
                return;

            if (oldDocument.GetLanguageService<ICopilotOptionsService>() is not { } service)
                return;

            if (await service.IsRefineOptionEnabledAsync().ConfigureAwait(false))
                await _lazyExternalCopilotService.Value.StartRefinementSessionAsync(oldDocument, newDocument, primaryDiagnostic, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
        {
            if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
                return string.Empty;

            return await _lazyExternalCopilotService.Value.GetOnTheFlyDocsAsync(symbolSignature, declarationCode, language, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> IsAnyExclusionAsync(CancellationToken cancellationToken)
        {
            if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
                return false;

            return await _lazyExternalCopilotService.Value.IsAnyExclusionAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
