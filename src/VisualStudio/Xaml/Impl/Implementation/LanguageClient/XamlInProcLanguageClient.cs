// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    /// <summary>
    /// Experimental XAML Language Server Client used everywhere when
    /// <see cref="XamlOptions.EnableLspIntelliSenseFeatureFlag"/> is turned on.
    /// </summary>
    [ContentType(ContentTypeNames.XamlContentType)]
    [Export(typeof(ILanguageClient))]
    internal class XamlInProcLanguageClient : AbstractInProcLanguageClient
    {
        private readonly XamlProjectService _projectService;
        private readonly IXamlLanguageServerFeedbackService? _feedbackService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public XamlInProcLanguageClient(
            XamlLspServiceProvider lspServiceProvider,
            IGlobalOptionService globalOptions,
            ILspServiceLoggerFactory lspLoggerFactory,
            IThreadingContext threadingContext,
            XamlProjectService projectService,
            [Import(AllowDefault = true)] IXamlLanguageServerFeedbackService? feedbackService)
            : base(lspServiceProvider, globalOptions, lspLoggerFactory, threadingContext)
        {
            _projectService = projectService;
            _feedbackService = feedbackService;
        }

        protected override ImmutableArray<string> SupportedLanguages => ImmutableArray.Create(StringConstants.XamlLanguageName);

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var isLspExperimentEnabled = IsXamlLspIntelliSenseEnabled();

            return isLspExperimentEnabled ? XamlCapabilities.Current : XamlCapabilities.None;
        }

        public override AbstractLanguageServer<RequestContext> Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            WellKnownLspServerKinds serverKind,
            ILspServiceLogger logger)
        {
            return new XamlLanguageServer(LspServiceProvider, jsonRpc, capabilitiesProvider, logger, SupportedLanguages, serverKind, _projectService, _feedbackService);
        }

        /// <summary>
        /// Failures are only catastrophic when this server is providing intellisense features.
        /// </summary>
        public override bool ShowNotificationOnInitializeFailed => IsXamlLspIntelliSenseEnabled();

        public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.XamlLspServer;

        private bool IsXamlLspIntelliSenseEnabled()
            => GlobalOptions.GetOption(XamlOptions.EnableLspIntelliSenseFeatureFlag);
    }
}
