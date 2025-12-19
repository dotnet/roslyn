// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Service to create <see cref="LspMiscFilesProvider"/> instances.
/// This is not exported as a <see cref="ILspServiceFactory"/> as it requires
/// special base language server dependencies such as the <see cref="HostServices"/>
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(ILspMiscellaneousFilesWorkspaceProviderFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspMiscellaneousFilesWorkspaceProviderFactory() : ILspMiscellaneousFilesWorkspaceProviderFactory
{
    public ImmutableArray<ILspMiscellaneousFilesWorkspaceProvider> CreateLspMiscellaneousFilesWorkspaceProviders(ILspServices lspServices, HostServices hostServices)
    {
        // Return only the catch-all provider for the base implementation
        return [new LspMiscFilesProvider(lspServices, hostServices)];
    }
}
