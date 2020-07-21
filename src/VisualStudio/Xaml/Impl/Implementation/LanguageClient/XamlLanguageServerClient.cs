// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    [ContentType(ContentTypeNames.XamlContentType)]
    [ClientName(ClientName)]
    [DisableUserExperience(disableUserExperience: true)] // Remove this when we are ready to use LSP everywhere
    [Export(typeof(ILanguageClient))]
    internal class XamlLanguageServerClient : AbstractLanguageServerClient
    {
        public const string ClientName = "XAML";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public XamlLanguageServerClient(XamlLanguageServerProtocol languageServerProtocol, VisualStudioWorkspace workspace, IDiagnosticService diagnosticService)
            : base(languageServerProtocol, workspace, diagnosticService, ClientName)
        {
        }

        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public override string Name => Resources.Xaml_Language_Server_Client;
    }
}
