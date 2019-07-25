// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.WorkspaceExecuteCommandName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        public RoslynRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, IThreadingContext threadingContext)
            : base(codeFixService, codeRefactoringService, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.WorkspaceExecuteCommandName)]
    internal class CSharpRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        public CSharpRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, IThreadingContext threadingContext)
            : base(codeFixService, codeRefactoringService, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.WorkspaceExecuteCommandName)]
    internal class VisualBasicRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        public VisualBasicRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, IThreadingContext threadingContext)
            : base(codeFixService, codeRefactoringService, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.WorkspaceExecuteCommandName)]
    internal class TypeScriptRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        public TypeScriptRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, IThreadingContext threadingContext)
            : base(codeFixService, codeRefactoringService, threadingContext)
        {
        }
    }
}
