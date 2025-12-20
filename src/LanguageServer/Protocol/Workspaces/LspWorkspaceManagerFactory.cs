// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportCSharpVisualBasicLspServiceFactory(typeof(LspWorkspaceManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class LspWorkspaceManagerFactory(
    LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
    ILspMiscellaneousFilesWorkspaceProviderFactory? miscFilesProviderFactory,
    HostServices hostServices) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var logger = lspServices.GetRequiredService<AbstractLspLogger>();
        
        // Get all providers from the factory
        var miscFilesWorkspaceProviders = miscFilesProviderFactory != null
            ? miscFilesProviderFactory.CreateLspMiscellaneousFilesWorkspaceProviders(lspServices, hostServices)
            : ImmutableArray<ILspMiscellaneousFilesWorkspaceProvider>.Empty;
        
        var languageInfoProvider = lspServices.GetRequiredService<ILanguageInfoProvider>();
        var telemetryLogger = lspServices.GetRequiredService<RequestTelemetryLogger>();
        return new LspWorkspaceManager(logger, miscFilesWorkspaceProviders, lspWorkspaceRegistrationService, languageInfoProvider, telemetryLogger);
    }
}
