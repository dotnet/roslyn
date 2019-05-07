//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
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

#if !VS_16_0
    [ExportLanguageService(typeof(IEditorInlineRenameService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspRenameService : RoslynRenameService
    {
        [ImportingConstructor]
        public TypeScriptLspRenameService()
        {
        }
    }
#endif
}
