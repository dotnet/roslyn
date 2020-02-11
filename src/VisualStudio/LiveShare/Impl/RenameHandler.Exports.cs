﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentRenameName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynRenameHandler : RenameHandler
    {
        [ImportingConstructor]
        public RoslynRenameHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentRenameName)]
    internal class CSharpRenameHandler : RenameHandler
    {
        [ImportingConstructor]
        public CSharpRenameHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentRenameName)]
    internal class VisualBasicRenameHandler : RenameHandler
    {
        [ImportingConstructor]
        public VisualBasicRenameHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentRenameName)]
    internal class TypeScriptRenameHandler : RenameHandler
    {
        [ImportingConstructor]
        public TypeScriptRenameHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }
    }
}
