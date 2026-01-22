// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

// The C# and VB ILanguageClient should not activate on the host. When LiveShare mirrors the C# ILC to the guest,
// they will not copy the DisableUserExperience attribute, so guests will still use the C# ILC.
[DisableUserExperience(true)]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[Export(typeof(ILanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, true)]
internal sealed class LiveShareInProcLanguageClient(
    CSharpVisualBasicLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    DefaultCapabilitiesProvider experimentalCapabilitiesProvider,
    ILspServiceLoggerFactory lspLoggerFactory,
    IThreadingContext threadingContext,
    ExportProvider exportProvider) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, threadingContext, exportProvider)
{
    private readonly DefaultCapabilitiesProvider _experimentalCapabilitiesProvider = experimentalCapabilitiesProvider;

    protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

    public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var isLspEditorEnabled = GlobalOptions.GetOption(LspOptionsStorage.LspEditorFeatureFlag);

        // If the preview feature flag to turn on the LSP editor in local scenarios is on, advertise no capabilities for this Live Share
        // LSP server as LSP requests will be serviced by the AlwaysActiveInProcLanguageClient in both local and remote scenarios.
        if (isLspEditorEnabled)
        {
            return new VSServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = false,
                    Change = TextDocumentSyncKind.None,
                }
            };
        }

        var defaultCapabilities = _experimentalCapabilitiesProvider.GetCapabilities(clientCapabilities);

        // If the LSP semantic tokens feature flag is enabled, advertise no semantic tokens capabilities for this Live Share
        // LSP server as LSP semantic tokens requests will be serviced by the AlwaysActiveInProcLanguageClient in both local and
        // remote scenarios.
        var isLspSemanticTokenEnabled = GlobalOptions.GetOption(LspOptionsStorage.LspSemanticTokensFeatureFlag);
        if (isLspSemanticTokenEnabled)
        {
            defaultCapabilities.SemanticTokensOptions = null;
        }

        return defaultCapabilities;
    }

    /// <summary>
    /// Failures are catastrophic as liveshare guests will not have language features without this server.
    /// </summary>
    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.LiveShareLspServer;
}
