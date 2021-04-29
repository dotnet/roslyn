// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    /// <summary>
    /// XAML Language Server Client for LiveShare and Codespaces. Unused when
    /// <see cref="StringConstants.EnableLspIntelliSense"/> experiment is turned on.
    /// Remove this when we are ready to use LSP everywhere
    /// </summary>
    [DisableUserExperience(true)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [Export(typeof(ILanguageClient))]
    internal class XamlInProcLanguageClientDisableUX : AbstractInProcLanguageClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public XamlInProcLanguageClientDisableUX(
            XamlRequestDispatcherFactory xamlDispatcherFactory,
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            [Import(typeof(SAsyncServiceProvider))] VSShell.IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext)
            : base(xamlDispatcherFactory, workspace, diagnosticService, listenerProvider, lspWorkspaceRegistrationService, asyncServiceProvider, threadingContext, diagnosticsClientName: null)
        {
        }

        /// <summary>
        /// Gets the name of the language client (displayed in yellow bars).
        /// </summary>
        public override string Name => "XAML Language Server Client for LiveShare and Codespaces";

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var experimentationService = Workspace.Services.GetRequiredService<IExperimentationService>();
            var isLspExperimentEnabled = experimentationService.IsExperimentEnabled(StringConstants.EnableLspIntelliSense);

            var capabilities = isLspExperimentEnabled ? XamlCapabilities.None : XamlCapabilities.Current;

            // Only turn on CodeAction support for client scenarios. Hosts will get non-LSP lightbulbs automatically.
            capabilities.CodeActionProvider = new CodeActionOptions { CodeActionKinds = new[] { CodeActionKind.QuickFix, CodeActionKind.Refactor } };
            capabilities.CodeActionsResolveProvider = true;

            return capabilities;
        }
    }
}
