// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentDefinitionName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynGoToDefinitionHandler : AbstractGoToDefinitionWithFindUsagesServiceHandler
    {
        [ImportingConstructor]
        public RoslynGoToDefinitionHandler([Import(AllowDefault = true)] IMetadataAsSourceFileService metadataAsSourceService)
            : base(metadataAsSourceService)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDefinitionName)]
    internal class TypeScriptGoToDefinitionHandler : AbstractGoToDefinitionWithFindUsagesServiceHandler
    {
        [ImportingConstructor]
        public TypeScriptGoToDefinitionHandler() : base(null)
        {
        }
    }
}
