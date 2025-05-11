// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

/// <summary>
/// Language client to handle TS LSP requests.
/// Allows us to move features to LSP without being blocked by TS as well
/// as ensures that TS LSP features use correct solution snapshots.
/// </summary>
[ContentType(ContentTypeNames.TypeScriptContentTypeName)]
[ContentType(ContentTypeNames.JavaScriptContentTypeName)]
[Export(typeof(ILanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, true)]
internal class VSTypeScriptInProcLanguageClient(
    [Import(AllowDefault = true)] IVSTypeScriptCapabilitiesProvider? typeScriptCapabilitiesProvider,
    VSTypeScriptLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    ILspServiceLoggerFactory lspLoggerFactory,
    IThreadingContext threadingContext,
    ExportProvider exportProvider) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, threadingContext, exportProvider)
{
    private readonly IVSTypeScriptCapabilitiesProvider? _typeScriptCapabilitiesProvider = typeScriptCapabilitiesProvider;

    protected override ImmutableArray<string> SupportedLanguages => [InternalLanguageNames.TypeScript];

    public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var serverCapabilities = GetTypeScriptServerCapabilities(clientCapabilities);

        serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            Change = TextDocumentSyncKind.Incremental,
            OpenClose = true,
        };

        serverCapabilities.ProjectContextProvider = true;

        serverCapabilities.SupportsDiagnosticRequests = true;
        serverCapabilities.DiagnosticProvider = new()
        {
            SupportsMultipleContextsDiagnostics = true,
            DiagnosticKinds =
            [
                new(PullDiagnosticCategories.Task),
                new(PullDiagnosticCategories.WorkspaceDocumentsAndProject),
                new(PullDiagnosticCategories.DocumentAnalyzerSyntax),
                new(PullDiagnosticCategories.DocumentAnalyzerSemantic),
            ]
        };

        return serverCapabilities;
    }

    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.RoslynTypeScriptLspServer;

    private VSInternalServerCapabilities GetTypeScriptServerCapabilities(ClientCapabilities clientCapabilities)
    {
        if (_typeScriptCapabilitiesProvider != null)
        {
            var serializedClientCapabilities = JsonConvert.SerializeObject(clientCapabilities);
            var serializedServerCapabilities = _typeScriptCapabilitiesProvider.GetServerCapabilities(serializedClientCapabilities);
            var typeScriptServerCapabilities = JsonConvert.DeserializeObject<VSInternalServerCapabilities>(serializedServerCapabilities);
            Contract.ThrowIfNull(typeScriptServerCapabilities);
            return typeScriptServerCapabilities;
        }
        else
        {
            return new VSInternalServerCapabilities();
        }
    }
}
