// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.CodeActions
{
    [ExportCodeRefactoringProvider(StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspCodeActionProvider : RoslynCodeActionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspCodeActionProvider(CSharpLspClientServiceFactory csharpLspClientServiceFactory, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(csharpLspClientServiceFactory, diagnosticAnalyzerService)
        {
        }
    }

    [ExportCodeRefactoringProvider(StringConstants.VBLspLanguageName), Shared]
    internal class VBLspCodeActionProvider : RoslynCodeActionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VBLspCodeActionProvider(VisualBasicLspClientServiceFactory vbLspClientServiceFactory, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(vbLspClientServiceFactory, diagnosticAnalyzerService)
        {
        }
    }
}
