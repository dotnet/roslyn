// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentDefinitionName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynGoToDefinitionHandler : AbstractGoToDefinitionWithFindUsagesServiceHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynGoToDefinitionHandler([Import(AllowDefault = true)] IMetadataAsSourceFileService metadataAsSourceService,
            ILspSolutionProvider solutionProvider) : base(metadataAsSourceService, solutionProvider)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDefinitionName)]
    internal class TypeScriptGoToDefinitionHandler : AbstractGoToDefinitionWithFindUsagesServiceHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptGoToDefinitionHandler(ILspSolutionProvider solutionProvider) : base(null, solutionProvider)
        {
        }
    }
}
