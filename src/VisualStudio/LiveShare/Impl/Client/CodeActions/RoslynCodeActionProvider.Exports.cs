// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.Cascade.Common;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [ExportCodeRefactoringProvider(StringConstants.CSharpLspLanguageName)]
    internal class CSharpLspCodeActionProvider : RoslynCodeActionProvider
    {
        [ImportingConstructor]
        public CSharpLspCodeActionProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IVsConfigurationSettings configurationSettings, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(roslynLSPClientServiceFactory, configurationSettings, diagnosticAnalyzerService)
        {
        }
    }

    [ExportCodeRefactoringProvider(StringConstants.VBLspLanguageName)]
    internal class VBLspCodeActionProvider : RoslynCodeActionProvider
    {
        [ImportingConstructor]
        public VBLspCodeActionProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IVsConfigurationSettings configurationSettings, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(roslynLSPClientServiceFactory, configurationSettings, diagnosticAnalyzerService)
        {
        }
    }

#if !VS_16_0
    [ExportCodeRefactoringProvider(StringConstants.TypeScriptLanguageName)]
    internal class TypeScriptLspCodeActionProvider : RoslynCodeActionProvider
    {
        [ImportingConstructor]
        public TypeScriptLspCodeActionProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IVsConfigurationSettings configurationSettings, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(roslynLSPClientServiceFactory, configurationSettings, diagnosticAnalyzerService)
        {
        }
    }
#endif
}
