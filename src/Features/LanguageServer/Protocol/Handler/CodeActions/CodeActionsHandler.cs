// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handles the initial request for code actions. Leaves the Edit and Command properties
    /// of the returned VSCodeActions blank, as these properties should be populated by the
    /// CodeActionsResolveHandler only when the user requests them.
    /// </summary>
    internal class CodeActionsHandler : IRequestHandler<LSP.CodeActionParams, LSP.CodeAction[]>
    {
        private readonly CodeActionsCache _codeActionsCache;
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;

        internal const string RunCodeActionCommandName = "Roslyn.RunCodeAction";

        public string Method => LSP.Methods.TextDocumentCodeActionName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public CodeActionsHandler(
            CodeActionsCache codeActionsCache,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService)
        {
            _codeActionsCache = codeActionsCache;
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(CodeActionParams request) => request.TextDocument;

        public async Task<LSP.CodeAction[]> HandleRequestAsync(LSP.CodeActionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return Array.Empty<VSInternalCodeAction>();
            }

            var codeActions = await CodeActionHelpers.GetVSCodeActionsAsync(
                request, _codeActionsCache, document, _codeFixService, _codeRefactoringService, cancellationToken).ConfigureAwait(false);

            return codeActions;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly CodeActionsHandler _codeActionsHandler;

            public TestAccessor(CodeActionsHandler codeActionsHandler)
                => _codeActionsHandler = codeActionsHandler;

            public CodeActionsCache GetCache()
                => _codeActionsHandler._codeActionsCache;
        }
    }
}
