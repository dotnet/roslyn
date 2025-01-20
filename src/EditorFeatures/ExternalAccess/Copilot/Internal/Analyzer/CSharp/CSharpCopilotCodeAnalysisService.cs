// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

[ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared]
internal sealed partial class CSharpCopilotCodeAnalysisService : AbstractCopilotCodeAnalysisService
{
    private readonly Lazy<IExternalCSharpCopilotCodeAnalysisService> _lazyExternalCopilotService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotCodeAnalysisService(
        [Import(AllowDefault = true)] IExternalCSharpCopilotCodeAnalysisService? externalCopilotService,
        IDiagnosticsRefresher diagnosticsRefresher,
        SVsServiceProvider serviceProvider,
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer
        ) : base(diagnosticsRefresher)
    {
        _lazyExternalCopilotService = new Lazy<IExternalCSharpCopilotCodeAnalysisService>(GetExternalService, LazyThreadSafetyMode.PublicationOnly);

        IExternalCSharpCopilotCodeAnalysisService GetExternalService()
            => externalCopilotService ?? new ReflectionWrapper(serviceProvider, brokeredServiceContainer);
    }

    protected override Task<ImmutableArray<Diagnostic>> AnalyzeDocumentCoreAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
        => _lazyExternalCopilotService.Value.AnalyzeDocumentAsync(document, span, promptTitle, cancellationToken);

    protected override Task<ImmutableArray<string>> GetAvailablePromptTitlesCoreAsync(Document document, CancellationToken cancellationToken)
        => _lazyExternalCopilotService.Value.GetAvailablePromptTitlesAsync(document, cancellationToken);

    protected override Task<ImmutableArray<Diagnostic>> GetCachedDiagnosticsCoreAsync(Document document, string promptTitle, CancellationToken cancellationToken)
        => _lazyExternalCopilotService.Value.GetCachedDiagnosticsAsync(document, promptTitle, cancellationToken);

    protected override Task<bool> IsAvailableCoreAsync(CancellationToken cancellationToken)
        => _lazyExternalCopilotService.Value.IsAvailableAsync(cancellationToken);

    protected override Task StartRefinementSessionCoreAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
        => _lazyExternalCopilotService.Value.StartRefinementSessionAsync(oldDocument, newDocument, primaryDiagnostic, cancellationToken);

    protected override Task<string> GetOnTheFlyDocsCoreAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
        => _lazyExternalCopilotService.Value.GetOnTheFlyDocsAsync(symbolSignature, declarationCode, language, cancellationToken);

    protected override async Task<ImmutableArray<Diagnostic>> GetDiagnosticsIntersectWithSpanAsync(
        Document document, IReadOnlyList<Diagnostic> diagnostics, TextSpan span, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var filteredDiagnostics);

        // The location of Copilot diagnostics is on the method identifier, we'd like to expand the range to include them
        // if any part of the method intersects with the given span.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        foreach (var diagnostic in diagnostics)
        {
            var containingMethod = syntaxFacts.GetContainingMethodDeclaration(root, diagnostic.Location.SourceSpan.Start, useFullSpan: false);
            if (containingMethod?.Span.IntersectsWith(span) is true)
                filteredDiagnostics.Add(diagnostic);
        }

        return filteredDiagnostics.ToImmutable();
    }

    protected override Task<bool> IsFileExcludedCoreAsync(string filePath, CancellationToken cancellationToken)
        => _lazyExternalCopilotService.Value.IsFileExcludedAsync(filePath, cancellationToken);
}
