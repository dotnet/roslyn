// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

/// <summary>
/// A language server that handles requests .razor and .cshtml files. Endpoints and required services are supplied
/// by the Razor tooling team in the Razor tooling repo.
/// </summary>
[ContentType(Constants.RazorLSPContentType)]
[Export(typeof(ILanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorCohostLanguageClient(
    RazorLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    IThreadingContext threadingContext,
    ILspServiceLoggerFactory lspLoggerFactory,
    ExportProvider exportProvider,
    [Import(AllowDefault = true)] IRazorCohostCapabilitiesProvider? razorCapabilitiesProvider = null,
    [Import(AllowDefault = true)] IRazorCustomMessageTarget? razorCustomMessageTarget = null,
    [Import(AllowDefault = true)] IRazorCohostLanguageClientActivationService? razorCohostLanguageClientActivationService = null)
    : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, threadingContext, exportProvider, middleLayer: null)
{
    protected override ImmutableArray<string> SupportedLanguages => Constants.RazorLanguage;

    public override object? CustomMessageTarget => razorCustomMessageTarget;

    public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        if (razorCohostLanguageClientActivationService?.ShouldActivateCohostServer() != true)
        {
            // A default return would still opt us in to TextDocumentSync for open files, but even that's
            // wasteful if we don't want to handle anything, as we'd track document content.
            return new()
            {
                TextDocumentSync = new TextDocumentSyncOptions { OpenClose = false, Change = TextDocumentSyncKind.None, Save = false, WillSave = false, WillSaveWaitUntil = false }
            };
        }

        Contract.ThrowIfNull(razorCapabilitiesProvider);

        // We use a string to pass capabilities to/from Razor to avoid version issues with the Protocol DLL
        var serializedClientCapabilities = JsonConvert.SerializeObject(clientCapabilities);
        var serializedServerCapabilities = razorCapabilitiesProvider.GetCapabilities(serializedClientCapabilities);
        var razorCapabilities = JsonConvert.DeserializeObject<VSInternalServerCapabilities>(serializedServerCapabilities);
        Contract.ThrowIfNull(razorCapabilities);

        // We support a few things on this side, so lets make sure they're set
        razorCapabilities.ProjectContextProvider = true;
        razorCapabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            OpenClose = true,
            Change = TextDocumentSyncKind.Incremental
        };

        return razorCapabilities;
    }

    /// <summary>
    /// If the cohost server is expected to activate then any failures are catastrophic as no razor features will work.
    /// </summary>
    public override bool ShowNotificationOnInitializeFailed => razorCohostLanguageClientActivationService?.ShouldActivateCohostServer() == true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.RazorCohostServer;
}
