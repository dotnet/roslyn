// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(RoslynMethods.CodeActionPreviewName)]
    internal class PreviewCodeActionsHandler : CodeActionsHandlerBase, IRequestHandler<RunCodeActionParams, LSP.TextEdit[]>
    {
        [ImportingConstructor]
        public PreviewCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService)
            : base(codeFixService, codeRefactoringService)
        {
        }

        public async Task<LSP.TextEdit[]> HandleRequestAsync(Solution solution, RunCodeActionParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var edits = ArrayBuilder<LSP.TextEdit>.GetInstance();

            var codeActions = await GetCodeActionsAsync(solution,
                                                        request.TextDocument.Uri,
                                                        request.Range,
                                                        cancellationToken).ConfigureAwait(false);

            var actionToRun = codeActions?.FirstOrDefault(a => a.Title == request.Title);

            if (actionToRun != null)
            {
                var operations = await actionToRun.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                var applyChangesOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

                var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
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
