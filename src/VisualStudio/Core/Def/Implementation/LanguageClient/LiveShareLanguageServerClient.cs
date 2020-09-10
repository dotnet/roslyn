// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    // The C# and VB ILanguageClient should not activate on the host. When LiveShare mirrors the C# ILC to the guest, they will not copy the DisableUserExperience attribute,
    // so guests will still use the C# ILC.
    [DisableUserExperience(true)]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [Export(typeof(ILanguageClient))]
    internal class LiveShareLanguageServerClient : AbstractLanguageServerClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public LiveShareLanguageServerClient(LanguageServerProtocol languageServerProtocol,
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(languageServerProtocol, workspace, diagnosticService, listenerProvider, diagnosticsClientName: null)
        {
        }

        public override string Name => ServicesVSResources.Live_Share_CSharp_Visual_Basic_Language_Server_Client;
    }
}
