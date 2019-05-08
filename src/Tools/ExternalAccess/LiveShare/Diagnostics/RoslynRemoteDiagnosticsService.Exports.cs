// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [ExportLanguageService(typeof(IRemoteDiagnosticsService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspRemoteDiagnosticsService : RoslynRemoteDiagnosticsService
    {
        [ImportingConstructor]
        public CSharpLspRemoteDiagnosticsService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(IRemoteDiagnosticsService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspRemoteDiagnosticsService : RoslynRemoteDiagnosticsService
    {
        [ImportingConstructor]
        public VBLspRemoteDiagnosticsService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }
}
