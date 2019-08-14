// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentDefinitionName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynGoToDefinitionWithFarHandler : GotoDefinitionHandler
    {
        [ImportingConstructor]
        public RoslynGoToDefinitionWithFarHandler([Import(AllowDefault = true)] IMetadataAsSourceFileService metadataAsSourceService) : base(metadataAsSourceService)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentDefinitionName)]
    internal class CSharpGoToDefinitionWithFarHandler : GotoDefinitionHandler
    {
        [ImportingConstructor]
        public CSharpGoToDefinitionWithFarHandler([Import(AllowDefault = true)] IMetadataAsSourceFileService metadataAsSourceService) : base(metadataAsSourceService)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentDefinitionName)]
    internal class VisualBasicGoToDefinitionWithFarHandler : GotoDefinitionHandler
    {
        [ImportingConstructor]
        public VisualBasicGoToDefinitionWithFarHandler([Import(AllowDefault = true)] IMetadataAsSourceFileService metadataAsSourceService) : base(metadataAsSourceService)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDefinitionName)]
    internal class TypeScriptGoToDefinitionWithFarHandler : GotoDefinitionHandler
    {
        [ImportingConstructor]
        public TypeScriptGoToDefinitionWithFarHandler([Import(AllowDefault = true)] IMetadataAsSourceFileService metadataAsSourceService) : base(metadataAsSourceService)
        {
        }
    }
}
