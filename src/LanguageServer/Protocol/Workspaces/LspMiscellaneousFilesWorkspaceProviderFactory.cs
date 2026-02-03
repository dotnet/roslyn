// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Service to create <see cref="LspMiscellaneousFilesWorkspaceProvider"/> instances.
/// This is not exported as a <see cref="ILspServiceFactory"/> as it requires
/// special base language server dependencies such as the <see cref="HostServices"/>
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(ILspMiscellaneousFilesWorkspaceProviderFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspMiscellaneousFilesWorkspaceProviderFactory(IGlobalOptionService globalOptionService) : ILspMiscellaneousFilesWorkspaceProviderFactory
{
    public ILspMiscellaneousFilesWorkspaceProvider CreateLspMiscellaneousFilesWorkspaceProvider(ILspServices lspServices, HostServices hostServices)
    {
        return new LspMiscellaneousFilesWorkspaceProvider(lspServices, hostServices, globalOptionService);
    }
}
