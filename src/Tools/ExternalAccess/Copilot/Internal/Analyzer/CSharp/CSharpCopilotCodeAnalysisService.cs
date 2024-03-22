// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
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
}
