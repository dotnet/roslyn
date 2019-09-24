// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Rename
{
    [ExportLanguageService(typeof(IEditorInlineRenameService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspRenameService : RoslynRenameService
    {
        public CSharpLspRenameService()
        {
        }
    }

    [ExportLanguageService(typeof(IEditorInlineRenameService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspRenameService : RoslynRenameService
    {
        public VBLspRenameService()
        {
        }
    }
}
