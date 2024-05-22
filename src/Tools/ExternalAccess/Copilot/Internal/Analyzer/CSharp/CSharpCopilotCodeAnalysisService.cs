// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

internal sealed partial class CSharpCopilotCodeAnalysisService(
    Lazy<IExternalCopilotCodeAnalysisService> lazyExternalCopilotService,
    IDiagnosticsRefresher diagnosticsRefresher,
    VisualStudioCopilotOptionService copilotOptionService) : AbstractCopilotCodeAnalysisService(lazyExternalCopilotService, diagnosticsRefresher)
{
    private const string CopilotRefineOptionName = "EnableCSharpRefineQuickActionSuggestion";
    private const string CopilotCodeAnalysisOptionName = "EnableCSharpCodeAnalysis";

    public static CSharpCopilotCodeAnalysisService Create(
        HostLanguageServices languageServices,
        IDiagnosticsRefresher diagnosticsRefresher,
        VisualStudioCopilotOptionService copilotOptionService,
        SVsServiceProvider serviceProvider,
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer)
    {
        var lazyExternalCopilotService = new Lazy<IExternalCopilotCodeAnalysisService>(GetExternalService, LazyThreadSafetyMode.PublicationOnly);
        return new CSharpCopilotCodeAnalysisService(lazyExternalCopilotService, diagnosticsRefresher, copilotOptionService);

        IExternalCopilotCodeAnalysisService GetExternalService()
            => languageServices.GetService<IExternalCopilotCodeAnalysisService>() ?? new ReflectionWrapper(serviceProvider, brokeredServiceContainer);
    }

    public override Task<bool> IsRefineOptionEnabledAsync()
        => copilotOptionService.IsCopilotOptionEnabledAsync(CopilotRefineOptionName);

    public override Task<bool> IsCodeAnalysisOptionEnabledAsync()
        => copilotOptionService.IsCopilotOptionEnabledAsync(CopilotCodeAnalysisOptionName);

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
}
