// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.LiveShare;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [Shared]
    [ExportSignatureHelpProvider("CSharpLspSignatureHelpProvider", StringConstants.CSharpLspLanguageName)]
    internal class CSharpLspSignatureHelpProvider : RoslynSignatureHelpProvider
    {
        [ImportingConstructor]
        public CSharpLspSignatureHelpProvider(CSharpLspClientServiceFactory csharpLspClientServiceFactory)
            : base(csharpLspClientServiceFactory)
        {
        }
    }

    [Shared]
    [ExportSignatureHelpProvider("VBLspSignatureHelpProvider", StringConstants.VBLspLanguageName)]
    internal class VBLspSignatureHelpProvider : RoslynSignatureHelpProvider
    {
        [ImportingConstructor]
        public VBLspSignatureHelpProvider(VisualBasicLspClientServiceFactory vbLspClientServiceFactory)
            : base(vbLspClientServiceFactory)
        {
        }
    }
}
