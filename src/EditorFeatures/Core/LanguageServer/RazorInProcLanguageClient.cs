// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

/// <summary>
/// Defines the LSP server for Razor C#.  This is separate so that we can
/// activate this outside of a liveshare session and publish diagnostics
/// only for razor cs files.
/// TODO - This can be removed once C# is using LSP for diagnostics.
/// https://github.com/dotnet/roslyn/issues/42630
/// </summary>
/// <remarks>
/// This specifies RunOnHost because in LiveShare we don't want this to activate on the guest instance
/// because LiveShare drops the ClientName when it mirrors guest clients, so this client ends up being
/// activated solely by its content type, which means it receives requests for normal .cs and .vb files
/// even for non-razor projects, which then of course fails because it gets text sync info for documents
/// it doesn't know about.
/// </remarks>
[ContentType(ContentTypeNames.CSharpContentType)]
[ClientName(ClientName)]
[RunOnContext(RunningContext.RunOnHost)]
[Export(typeof(ILanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RazorInProcLanguageClient(
    CSharpVisualBasicLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    ExperimentalCapabilitiesProvider experimentalCapabilitiesProvider,
    IThreadingContext threadingContext,
    ILspServiceLoggerFactory lspLoggerFactory,
    ExportProvider exportProvider,
    [Import(AllowDefault = true)] AbstractLanguageClientMiddleLayer middleLayer) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, threadingContext, exportProvider, middleLayer)
{
    public const string ClientName = ProtocolConstants.RazorCSharp;

    private readonly ExperimentalCapabilitiesProvider _experimentalCapabilitiesProvider = experimentalCapabilitiesProvider;

    protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

    protected override void Activate_OffUIThread()
    {
        // Ensure we let the default capabilities provider initialize off the UI thread to avoid
        // unnecessary MEF part loading during the GetCapabilities call, which is done on the UI thread
        _experimentalCapabilitiesProvider.Initialize();
    }

    public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var capabilities = _experimentalCapabilitiesProvider.GetCapabilities(clientCapabilities);

        // Razor doesn't use workspace symbols, so disable to prevent duplicate results (with LiveshareLanguageClient) in liveshare.
        capabilities.WorkspaceSymbolProvider = false;

        if (capabilities is VSInternalServerCapabilities vsServerCapabilities)
        {
            vsServerCapabilities.SupportsDiagnosticRequests = true;
            vsServerCapabilities.SpellCheckingProvider = true;
            vsServerCapabilities.Experimental ??= new Dictionary<string, bool>();
            vsServerCapabilities.MapCodeProvider = true;
            var experimental = (Dictionary<string, bool>)vsServerCapabilities.Experimental;
            experimental[SimplifyMethodHandler.SimplifyMethodMethodName] = true;
            experimental[FormatNewFileHandler.FormatNewFileMethodName] = true;
            experimental[SemanticTokensRangesHandler.SemanticRangesMethodName] = true;

            var regexExpression = string.Join("|", InlineCompletionsHandler.BuiltInSnippets);
            var regex = new Regex(regexExpression, RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
            vsServerCapabilities.InlineCompletionOptions = new VSInternalInlineCompletionOptions
            {
                Pattern = regex
            };

            return vsServerCapabilities;
        }

        return capabilities;
    }

    /// <summary>
    /// If the razor server is activated then any failures are catastrophic as no razor c# features will work.
    /// </summary>
    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.RazorLspServer;
}
