// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class PreviewCodeActionsHandler : ILspRequestHandler<RunCodeActionParams, LSP.TextEdit[], Solution>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly ILspSolutionProvider _solutionProvider;

        public PreviewCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, ILspSolutionProvider solutionProvider)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _solutionProvider = solutionProvider;
        }

        public async Task<LSP.TextEdit[]> HandleAsync(RunCodeActionParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var edits = ArrayBuilder<LSP.TextEdit>.GetInstance();
            var document = _solutionProvider.GetDocument(request.CodeActionParams.TextDocument);
            var codeActions = await CodeActionsHandler.GetCodeActionsAsync(document,
                _codeFixService,
                _codeRefactoringService,
                request.CodeActionParams.Range,
                cancellationToken).ConfigureAwait(false);

            var actionToRun = codeActions?.FirstOrDefault(a => a.Title == request.Title);

            if (actionToRun != null)
            {
                var operations = await actionToRun.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                var applyChangesOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                if (applyChangesOperation != null && document != null)
                {
                    var newSolution = applyChangesOperation.ChangedSolution;
                    var newDocument = newSolution.GetDocument(document.Id);

                    var textChanges = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);

                    edits.AddRange(textChanges.Select(tc => new LSP.TextEdit
                    {
                        NewText = tc.NewText,
                        Range = ProtocolConversions.TextSpanToRange(tc.Span, text)
                    }));
                }
            }

            return edits.ToArray();
        }
    }
}
