// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [Shared]
    [ExportSignatureHelpProvider("CSharpLspSignatureHelpProvider", StringConstants.CSharpLspLanguageName)]
    internal class CSharpLspSignatureHelpProvider : RoslynSignatureHelpProvider
    {
        [ImportingConstructor]
        public CSharpLspSignatureHelpProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }

    [Shared]
    [ExportSignatureHelpProvider("VBLspSignatureHelpProvider", StringConstants.VBLspLanguageName)]
    internal class VBLspSignatureHelpProvider : RoslynSignatureHelpProvider
    {
        [ImportingConstructor]
        public VBLspSignatureHelpProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }
}
