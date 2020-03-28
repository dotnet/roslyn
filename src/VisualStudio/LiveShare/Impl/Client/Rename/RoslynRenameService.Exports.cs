// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Rename
{
    [ExportLanguageService(typeof(IEditorInlineRenameService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspRenameService : RoslynRenameService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspRenameService()
        {
        }
    }

    [ExportLanguageService(typeof(IEditorInlineRenameService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspRenameService : RoslynRenameService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VBLspRenameService()
        {
        }
    }
}
