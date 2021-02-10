// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Exports all the code action handlers together to ensure they
    /// share the same code actions cache state.
    /// </summary>
    [ExportLspRequestHandlerProvider, Shared]
    [ProvidesMethod(LSP.Methods.TextDocumentCodeActionName)]
    [ProvidesMethod(LSP.MSLSPMethods.TextDocumentCodeActionResolveName)]
    [ProvidesCommand(CodeActionsHandler.RunCodeActionCommandName)]
    internal class CodeActionsHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsHandlerProvider(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IThreadingContext threadingContext)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _threadingContext = threadingContext;
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
        {
            var codeActionsCache = new CodeActionsCache();
            return ImmutableArray.Create<IRequestHandler>(
                new CodeActionsHandler(codeActionsCache, _codeFixService, _codeRefactoringService),
                new CodeActionResolveHandler(codeActionsCache, _codeFixService, _codeRefactoringService),
                new RunCodeActionHandler(codeActionsCache, _codeFixService, _codeRefactoringService, _threadingContext));
        }
    }
}
