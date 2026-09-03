// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export(typeof(CSharpVisualBasicLanguageServerFactory)), Shared]
internal sealed class CSharpVisualBasicLanguageServerFactory
{
    private readonly AbstractLspServiceProvider _lspServiceProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpVisualBasicLanguageServerFactory(
        CSharpVisualBasicLspServiceProvider lspServiceProvider)
    {
        _lspServiceProvider = lspServiceProvider;
    }

    public AbstractLanguageServer<RequestContext> Create(
        JsonRpc jsonRpc,
        JsonSerializerOptions options,
        WellKnownLspServerKinds serverKind,
        HostServices hostServices,
        AbstractTypeRefResolver? typeRefResolver = null)
    {
        var server = new RoslynLanguageServer(
            _lspServiceProvider,
            jsonRpc,
            options,
            hostServices,
            ProtocolConstants.RoslynLspLanguages,
            serverKind,
            typeRefResolver);

        return server;
    }
}
