// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using CustomMethods = Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol.CustomMethods;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [Export(LiveShareConstants.RoslynContractName, typeof(ILspNotificationProvider))]
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, CustomMethods.GetDocumentDiagnosticsName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynDiagnosticsHandler : DiagnosticsHandler
    {
        [ImportingConstructor]
        public RoslynDiagnosticsHandler(IDiagnosticService diagnosticService)
            : base(diagnosticService)
        {
        }
    }

    [Export(LiveShareConstants.CSharpContractName, typeof(ILspNotificationProvider))]
    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, CustomMethods.GetDocumentDiagnosticsName)]
    internal class CSharpDiagnosticsHandler : DiagnosticsHandler
    {
        [ImportingConstructor]
        public CSharpDiagnosticsHandler(IDiagnosticService diagnosticService)
            : base(diagnosticService)
        {
        }
    }

    [Export(LiveShareConstants.VisualBasicContractName, typeof(ILspNotificationProvider))]
    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, CustomMethods.GetDocumentDiagnosticsName)]
    internal class VisualBasicDiagnosticsHandler : DiagnosticsHandler
    {
        [ImportingConstructor]
        public VisualBasicDiagnosticsHandler(IDiagnosticService diagnosticService)
            : base(diagnosticService)
        {
        }
    }

    [Export(LiveShareConstants.TypeScriptContractName, typeof(ILspNotificationProvider))]
    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, CustomMethods.GetDocumentDiagnosticsName)]
    internal class TypeScriptDiagnosticsHandler : DiagnosticsHandler
    {
        [ImportingConstructor]
        public TypeScriptDiagnosticsHandler(IDiagnosticService diagnosticService)
            : base(diagnosticService)
        {
        }
    }

    /// <summary>
    /// <see cref="LiveShareConstants.RoslynLSPSDKContractName"/> is only used for typescript.
    /// </summary>
    [Export(LiveShareConstants.RoslynLSPSDKContractName, typeof(ILspNotificationProvider))]
    [ExportLspRequestHandler(LiveShareConstants.RoslynLSPSDKContractName, CustomMethods.GetDocumentDiagnosticsName)]
    internal class RoslynLSPSDKDiagnosticsHandler : DiagnosticsHandler
    {
        [ImportingConstructor]
        public RoslynLSPSDKDiagnosticsHandler(IDiagnosticService diagnosticService)
            : base(diagnosticService)
        {
        }
    }
}
