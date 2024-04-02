// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

[ExportLanguageServiceFactory(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpCopilotCodeAnalysisServiceFactory(
    IDiagnosticsRefresher diagnosticsRefresher,
    VisualStudioCopilotOptionService copilotOptionService,
    SVsServiceProvider serviceProvider,
    IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer) : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        => CSharpCopilotCodeAnalysisService.Create(languageServices, diagnosticsRefresher, copilotOptionService, serviceProvider, brokeredServiceContainer);
}
