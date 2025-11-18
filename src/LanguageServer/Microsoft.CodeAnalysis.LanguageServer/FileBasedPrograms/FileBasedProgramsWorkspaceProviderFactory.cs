// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
/// Service to create <see cref="FileBasedProgramsProjectSystem"/> instances.
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
    IBinLogPathProvider binLogPathProvider) : ILspMiscellaneousFilesWorkspaceProviderFactory
{
    public ILspMiscellaneousFilesWorkspaceProvider CreateLspMiscellaneousFilesWorkspaceProvider(ILspServices lspServices, HostServices hostServices)
    {
        return new FileBasedProgramsProjectSystem(
            lspServices,
            projectXmlProvider,
            workspaceFactory,
            fileChangeWatcher,
            globalOptionService,
            loggerFactory,
            listenerProvider,
            projectLoadTelemetry,
            serverConfigurationFactory,
            binLogPathProvider);
    }
}
