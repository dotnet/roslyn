// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.CodeActionPreviewName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynPreviewCodeActionsHandler : PreviewCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynPreviewCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, ILspSolutionProvider solutionProvider)
            : base(codeFixService, codeRefactoringService, solutionProvider)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, RoslynMethods.CodeActionPreviewName)]
    internal class CSharpPreviewCodeActionsHandler : PreviewCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpPreviewCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, ILspSolutionProvider solutionProvider)
            : base(codeFixService, codeRefactoringService, solutionProvider)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, RoslynMethods.CodeActionPreviewName)]
    internal class VisualBasicPreviewCodeActionsHandler : PreviewCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualBasicPreviewCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, ILspSolutionProvider solutionProvider)
            : base(codeFixService, codeRefactoringService, solutionProvider)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, RoslynMethods.CodeActionPreviewName)]
    internal class TypeScriptPreviewCodeActionsHandler : PreviewCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptPreviewCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, ILspSolutionProvider solutionProvider)
            : base(codeFixService, codeRefactoringService, solutionProvider)
        {
        }
    }
}
