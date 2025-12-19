// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Service to create ordered list of <see cref="ILspMiscellaneousFilesWorkspaceProvider"/> instances.
/// This is not exported as a <see cref="ILspServiceFactory"/> as it requires
/// special base language server dependencies such as the <see cref="HostServices"/>
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(ILspMiscellaneousFilesWorkspaceProviderFactory), WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramsWorkspaceProviderFactory(
    VirtualProjectXmlProvider projectXmlProvider,
    LanguageServerWorkspaceFactory workspaceFactory,
    IFileChangeWatcher fileChangeWatcher,
    IGlobalOptionService globalOptionService,
    ILoggerFactory loggerFactory,
    IAsynchronousOperationListenerProvider listenerProvider,
    ProjectLoadTelemetryReporter projectLoadTelemetry,
    ServerConfigurationFactory serverConfigurationFactory,
    IBinLogPathProvider binLogPathProvider,
    DotnetCliHelper dotnetCliHelper) : ILspMiscellaneousFilesWorkspaceProviderFactory
{
    public ImmutableArray<ILspMiscellaneousFilesWorkspaceProvider> CreateLspMiscellaneousFilesWorkspaceProviders(ILspServices lspServices, HostServices hostServices)
    {
        // Return providers in priority order:
        // 1. File-based programs provider (handles files with #! or #: directives)
        // 2. Canonical misc files provider (handles files that can run design time builds)
        // 3. LSP misc files provider (catch-all for everything else, like Razor files)
        return
        [
            new FileBasedProgramsMiscFilesProvider(
                lspServices,
                projectXmlProvider,
                workspaceFactory,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binLogPathProvider,
                dotnetCliHelper),
            new CanonicalMiscFilesProvider(
                lspServices,
                workspaceFactory,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binLogPathProvider,
                dotnetCliHelper),
            new LspMiscFilesProvider(lspServices, hostServices)
        ];
    }
}
