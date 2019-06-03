﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [Export(LiveShareConstants.RoslynContractName, typeof(ILspNotificationProvider))]
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.GetDocumentDiagnosticsName)]
    internal class RoslynDiagnosticsHandler : DiagnosticsHandler
    {
        [ImportingConstructor]
        public RoslynDiagnosticsHandler(IDiagnosticService diagnosticService)
            : base(diagnosticService)
        {
        }
    }

    [Export(LiveShareConstants.RoslynLSPSDKContractName, typeof(ILspNotificationProvider))]
    [ExportLspRequestHandler(LiveShareConstants.RoslynLSPSDKContractName, Methods.GetDocumentDiagnosticsName)]
    internal class RoslynLSPSDKDiagnosticsHandler : DiagnosticsHandler
    {
        [ImportingConstructor]
        public RoslynLSPSDKDiagnosticsHandler(IDiagnosticService diagnosticService)
            : base(diagnosticService)
        {
        }
    }
}
