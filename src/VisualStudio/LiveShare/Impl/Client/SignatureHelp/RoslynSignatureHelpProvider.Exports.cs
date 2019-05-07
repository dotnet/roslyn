//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Composition;
using Microsoft.Cascade.Common;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [ExportSignatureHelpProvider("CSharpLspSignatureHelpProvider", StringConstants.CSharpLspLanguageName)]
    internal class CSharpLspSignatureHelpProvider : RoslynSignatureHelpProvider
    {
        [ImportingConstructor]
        public CSharpLspSignatureHelpProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IVsConfigurationSettings configurationSettings)
            : base(roslynLSPClientServiceFactory, configurationSettings )
        {
        }
    }

    [ExportSignatureHelpProvider("VBLspSignatureHelpProvider", StringConstants.VBLspLanguageName)]
    internal class VBLspSignatureHelpProvider : RoslynSignatureHelpProvider
    {
        [ImportingConstructor]
        public VBLspSignatureHelpProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IVsConfigurationSettings configurationSettings)
            : base(roslynLSPClientServiceFactory, configurationSettings)
        {
        }
    }

#if !VS_16_0
    [ExportSignatureHelpProvider("TypeScriptLspSignatureHelpProvider", StringConstants.TypeScriptLanguageName)]
    internal class TypeScriptLspSignatureHelpProvider : RoslynSignatureHelpProvider
    {
        [ImportingConstructor]
        public TypeScriptLspSignatureHelpProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IVsConfigurationSettings configurationSettings)
            : base(roslynLSPClientServiceFactory, configurationSettings)
        {
        }
    }
#endif
}
