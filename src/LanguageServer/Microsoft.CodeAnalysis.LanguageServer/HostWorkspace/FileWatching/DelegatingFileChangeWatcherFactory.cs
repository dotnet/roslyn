// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

[ExportCSharpVisualBasicLspServiceFactory(typeof(DelegatingFileChangeWatcher)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DelegatingFileChangeWatcherFactory(
    ILoggerFactory loggerFactory,
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new DelegatingFileChangeWatcher(lspServices, loggerFactory, asynchronousOperationListenerProvider);
}
