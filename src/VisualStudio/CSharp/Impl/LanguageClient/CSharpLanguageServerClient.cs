// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageClient
{
    // currently, platform doesn't allow multiple content types
    // to be associated with 1 ILanguageClient forcing us to
    // create multiple ILanguageClients for each content type
    // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/952373
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Export(typeof(ILanguageClient))]
    [ExportMetadata("Capabilities", "WorkspaceStreamingSymbolProvider")]
    internal class CSharpLanguageServerClient : AbstractLanguageServerClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLanguageServerClient(
            IThreadingContext threadingContext,
            VisualStudioWorkspace workspace,
            [ImportMany]IEnumerable<Lazy<IOptionPersister>> lazyOptions,
            LanguageServerClientEventListener eventListener,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(
                threadingContext,
                workspace,
                lazyOptions,
                eventListener,
                listenerProvider,
                languageServerName: WellKnownServiceHubServices.CSharpLanguageServer,
                serviceHubClientName: "ManagedLanguage.IDE.CSharpLanguageServer")
        {
        }

        public override string Name => CSharpVSResources.CSharp_language_server_client;
    }
}
