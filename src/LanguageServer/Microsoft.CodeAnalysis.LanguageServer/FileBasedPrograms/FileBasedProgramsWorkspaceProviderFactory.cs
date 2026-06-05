// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Service to create <see cref="FileBasedProgramsProjectSystem"/> instances.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(ILspMiscellaneousFilesWorkspaceProvider), WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramsWorkspaceProviderFactory(
    VirtualProjectXmlProvider projectXmlProvider,
    IGlobalOptionService globalOptionService,
    ILoggerFactory loggerFactory,
    IAsynchronousOperationListenerProvider listenerProvider,
    ServerConfigurationFactory serverConfigurationFactory,
    IBinLogPathProvider binLogPathProvider,
    DotnetCliHelper dotnetCliHelper) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new FileBasedProgramsProjectSystem(
            lspServices,
            projectXmlProvider,
            globalOptionService,
            loggerFactory,
            listenerProvider,
            serverConfigurationFactory,
            binLogPathProvider,
            dotnetCliHelper);
    }
}
