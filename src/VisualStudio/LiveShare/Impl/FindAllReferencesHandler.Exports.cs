// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentReferencesName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFindAllReferencesHandler : FindAllReferencesHandler
    {
        [ImportingConstructor]
        public RoslynFindAllReferencesHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentReferencesName)]
    internal class CSharpFindAllReferencesHandler : FindAllReferencesHandler
    {
        [ImportingConstructor]
        public CSharpFindAllReferencesHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentReferencesName)]
    internal class VisualBasicFindAllReferencesHandler : FindAllReferencesHandler
    {
        [ImportingConstructor]
        public VisualBasicFindAllReferencesHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentReferencesName)]
    internal class TypeScriptFindAllReferencesHandler : FindAllReferencesHandler
    {
        [ImportingConstructor]
        public TypeScriptFindAllReferencesHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }
}
