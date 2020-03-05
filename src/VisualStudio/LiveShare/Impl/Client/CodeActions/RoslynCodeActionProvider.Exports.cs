// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.CodeActions
{
    [Shared]
    [ExportCodeRefactoringProvider(StringConstants.CSharpLspLanguageName)]
    internal class CSharpLspCodeActionProvider : RoslynCodeActionProvider
    {
        [ImportingConstructor]
        public CSharpLspCodeActionProvider(CSharpLspClientServiceFactory csharpLspClientServiceFactory, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(csharpLspClientServiceFactory, diagnosticAnalyzerService)
        {
        }
    }

    [Shared]
    [ExportCodeRefactoringProvider(StringConstants.VBLspLanguageName)]
    internal class VBLspCodeActionProvider : RoslynCodeActionProvider
    {
        [ImportingConstructor]
        public VBLspCodeActionProvider(VisualBasicLspClientServiceFactory vbLspClientServiceFactory, IDiagnosticAnalyzerService diagnosticAnalyzerService)
            : base(vbLspClientServiceFactory, diagnosticAnalyzerService)
        {
        }
    }
}
