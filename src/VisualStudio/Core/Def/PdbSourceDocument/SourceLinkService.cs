// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.VisualStudio.Debugger.Contracts.SourceLink;
using Microsoft.VisualStudio.Debugger.Contracts.SymbolLocator;

namespace Microsoft.VisualStudio.LanguageServices.PdbSourceDocument;

[Export(typeof(ISourceLinkService)), Shared]
internal class SourceLinkService : AbstractSourceLinkService
{
    private readonly IDebuggerSymbolLocatorService _debuggerSymbolLocatorService;
    private readonly IDebuggerSourceLinkService _debuggerSourceLinkService;
    private readonly IPdbSourceDocumentLogger? _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SourceLinkService(
        IDebuggerSymbolLocatorService debuggerSymbolLocatorService,
        IDebuggerSourceLinkService debuggerSourceLinkService,
        [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger)
    {
        _debuggerSymbolLocatorService = debuggerSymbolLocatorService;
        _debuggerSourceLinkService = debuggerSourceLinkService;
        _logger = logger;
    }

    protected override async Task<SymbolLocatorResult?> LocateSymbolFileAsync(SymbolLocatorPdbInfo pdbInfo, SymbolLocatorSearchFlags flags, CancellationToken cancellationToken)
    {
        return await _debuggerSymbolLocatorService.LocateSymbolFileAsync(pdbInfo, flags, progress: null, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<SourceLinkResult?> GetSourceLinkAsync(string url, string relativePath, CancellationToken cancellationToken)
    {
        return await _debuggerSourceLinkService.GetSourceLinkAsync(url, relativePath, allowInteractiveLogin: false, cancellationToken).ConfigureAwait(false);
    }

    protected override IPdbSourceDocumentLogger? Logger => _logger;
}
