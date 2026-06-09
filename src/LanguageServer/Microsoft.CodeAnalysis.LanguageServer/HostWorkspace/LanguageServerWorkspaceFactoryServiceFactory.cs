// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// LSP service factory that constructs the per-LSP-server <see cref="LanguageServerWorkspaceFactory"/>.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(LanguageServerWorkspaceFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerWorkspaceFactoryServiceFactory(
    HostServicesProvider hostServicesProvider,
    ExtensionAssemblyManager extensionManager,
    [ImportMany] IEnumerable<IAnalyzerAssemblyRedirector> assemblyRedirectors,
    ILoggerFactory loggerFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new LanguageServerWorkspaceFactory(
            hostServicesProvider,
            lspServices,
            extensionManager,
            assemblyRedirectors,
            loggerFactory);
}
