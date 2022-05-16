﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
{
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
    internal class RazorInProcLanguageClient : AbstractInProcLanguageClient
    {
        public const string ClientName = ProtocolConstants.RazorCSharp;

        private readonly DefaultCapabilitiesProvider _defaultCapabilitiesProvider;

        protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorInProcLanguageClient(
            CSharpVisualBasicLspServiceProvider lspServiceProvider,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider,
            IThreadingContext threadingContext,
            ILspLoggerFactory lspLoggerFactory,
            [Import(AllowDefault = true)] AbstractLanguageClientMiddleLayer middleLayer)
            : base(lspServiceProvider, globalOptions, listenerProvider, lspLoggerFactory, threadingContext, middleLayer)
        {
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        protected override void Activate_OffUIThread()
        {
            // Ensure we let the default capabilities provider initialize off the UI thread to avoid
            // unnecessary MEF part loading during the GetCapabilities call, which is done on the UI thread
            _defaultCapabilitiesProvider.Initialize();
        }

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var capabilities = _defaultCapabilitiesProvider.GetCapabilities(clientCapabilities);

            // Razor doesn't use workspace symbols, so disable to prevent duplicate results (with LiveshareLanguageClient) in liveshare.
            capabilities.WorkspaceSymbolProvider = false;

            if (capabilities is VSInternalServerCapabilities vsServerCapabilities)
            {
                vsServerCapabilities.SupportsDiagnosticRequests = true;

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
}
