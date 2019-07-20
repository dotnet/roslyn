// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageClient
{
    // currently, platform doesn't allow multiple content types
    // to be associated with 1 ILanguageClient forcing us to
    // create multiple ILanguageClients for each content type
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Export(typeof(ILanguageClient))]
    [ExportMetadata("Capabilities", "WorkspaceStreamingSymbolProvider")]
    internal class CSharpLanguageServerClient : AbstractLanguageServerClient
    {
        [ImportingConstructor]
        public CSharpLanguageServerClient(VisualStudioWorkspace workspace)
            : base(workspace,
                   WellKnownServiceHubServices.CSharpLanguageServer,
                   "ManagedLanguage.IDE.CSharpLanguageServer")
        {
        }

        public override string Name => CSharpVSResources.CSharp_language_server_client;
    }
}
