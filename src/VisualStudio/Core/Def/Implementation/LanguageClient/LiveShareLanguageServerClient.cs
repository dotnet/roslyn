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
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    [ContentType(ContentTypeNames.CSharpLspContentTypeName)]
    [ContentType(ContentTypeNames.VBLspContentTypeName)]
    [Export(typeof(ILanguageClient))]
    internal class LiveShareLanguageServerClient : AbstractLanguageServerClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public LiveShareLanguageServerClient(LanguageServerProtocol languageServerProtocol, VisualStudioWorkspace workspace, IDiagnosticService diagnosticService)
            : base(languageServerProtocol, workspace, diagnosticService, diagnosticsClientName: null)
        {
        }

        public override string Name => ServicesVSResources.Live_Share_CSharp_Visual_Basic_Language_Server_Client;
    }
}
