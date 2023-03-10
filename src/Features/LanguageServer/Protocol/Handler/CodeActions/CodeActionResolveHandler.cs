// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Resolves a code action by filling out its Edit property. The handler is triggered only when a user hovers over a
    /// code action. This system allows the basic code action data to be computed quickly, and the complex data, to be
    /// computed only when necessary (i.e. when hovering/previewing a code action).
    /// <para>
    /// This system only supports text edits to documents.  In the future, supporting complex edits (including changes to
    /// project files) would be desirable.
    /// </para>
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(CodeActionResolveHandler)), Shared]
    [Method(LSP.Methods.CodeActionResolveName)]
    internal class CodeActionResolveHandler : ILspServiceDocumentRequestHandler<LSP.CodeAction, LSP.CodeAction>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionResolveHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IGlobalOptionService globalOptions)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _globalOptions = globalOptions;
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeAction request)
            => ((JToken)request.Data!).ToObject<CodeActionResolveData>()!.TextDocument;

        public async Task<LSP.CodeAction> HandleRequestAsync(LSP.CodeAction codeAction, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var solution = document.Project.Solution;

            var data = ((JToken)codeAction.Data!).ToObject<CodeActionResolveData>();
            Assumes.Present(data);

            var options = _globalOptions.GetCodeActionOptionsProvider();

            var codeActionsCache = context.GetRequiredLspService<CodeActionsCache>();
            var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
                codeActionsCache,
                document,
                data.Range,
                options,
                _codeFixService,
                _codeRefactoringService,
                cancellationToken).ConfigureAwait(false);

            var codeActionToResolve = CodeActionHelpers.GetCodeActionToResolve(data.UniqueIdentifier, codeActions);
            Contract.ThrowIfNull(codeActionToResolve);

            var operations = await codeActionToResolve.GetOperationsAsync(cancellationToken).ConfigureAwait(false);

            // TO-DO: We currently must execute code actions which add new documents on the server as commands,
            // since there is no LSP support for adding documents yet. In the future, we should move these actions
            // to execute on the client.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/

            var textDiffService = solution.Services.GetService<IDocumentTextDifferencingService>();

            using var _ = ArrayBuilder<TextDocumentEdit>.GetInstance(out var textDocumentEdits);

            foreach (var option in operations)
            {
                // We only support making solution-updating operations in LSP.  And only ones that modify documents. 1st
                // class code actions that do more than this are supposed to add the CodeAction.MakesNonDocumentChange
                // in their Tags so we can filter them out before returning them to the client.
                //
                // However, we cannot enforce this as 3rd party fixers can still run.  So we filter their results to 
                // only apply the portions of their work that updates documents, and nothing else.
                if (option is not ApplyChangesOperation applyChangesOperation)
                {
                    context.TraceInformation($"Skipping code action operation for '{data.UniqueIdentifier}'.  It was a '{option.GetType().FullName}'");
                    continue;
                }

                var changes = applyChangesOperation.ChangedSolution.GetChanges(solution);
                var projectChanges = changes.GetProjectChanges();

                // Ignore any non-document changes for now.  Note though that LSP does support additional functionality
                // (like create/rename/delete file).  Once VS updates their LSP client impl to support this, we should
                // add that support here.
                //
                // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceEdit
                //
                // Tracked with: https://github.com/dotnet/roslyn/issues/65303
#if false

                // TO-DO: If the change involves adding or removing a document, execute via command instead of WorkspaceEdit
                // until adding/removing documents is supported in LSP: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/
                // After support is added, remove the below if-statement and add code to support adding/removing documents.
                var addedDocuments = projectChanges.SelectMany(
                    pc => pc.GetAddedDocuments().Concat(pc.GetAddedAdditionalDocuments().Concat(pc.GetAddedAnalyzerConfigDocuments())));
                var removedDocuments = projectChanges.SelectMany(
                    pc => pc.GetRemovedDocuments().Concat(pc.GetRemovedAdditionalDocuments().Concat(pc.GetRemovedAnalyzerConfigDocuments())));
                if (addedDocuments.Any() || removedDocuments.Any())
                {
                    codeAction.Command = SetCommand(codeAction.Title, data);
                    return codeAction;
                }

                // TO-DO: If the change involves adding or removing a project reference, execute via command instead of
                // WorkspaceEdit until adding/removing project references is supported in LSP:
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1166040
                var projectReferences = projectChanges.SelectMany(
                    pc => pc.GetAddedProjectReferences().Concat(pc.GetRemovedProjectReferences()));
                if (projectReferences.Any())
                {
                    codeAction.Command = SetCommand(codeAction.Title, data);
                    return codeAction;
                }

#endif

                // Changed documents
                await AddTextDocumentEditsAsync(
                    projectChanges.SelectMany(pc => pc.GetChangedDocuments()),
                    applyChangesOperation.ChangedSolution.GetDocument,
                    solution.GetDocument).ConfigureAwait(false);

                // Changed analyzer config documents
                await AddTextDocumentEditsAsync(
                    projectChanges.SelectMany(pc => pc.GetChangedAnalyzerConfigDocuments()),
                    applyChangesOperation.ChangedSolution.GetAnalyzerConfigDocument,
                    solution.GetAnalyzerConfigDocument).ConfigureAwait(false);

                // Changed additional documents
                await AddTextDocumentEditsAsync(
                    projectChanges.SelectMany(pc => pc.GetChangedAdditionalDocuments()),
                    applyChangesOperation.ChangedSolution.GetAdditionalDocument,
                    solution.GetAdditionalDocument).ConfigureAwait(false);
            }

            codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = textDocumentEdits.ToArray() };

            return codeAction;

            async Task AddTextDocumentEditsAsync<TTextDocument>(
                IEnumerable<DocumentId> changedDocuments,
                Func<DocumentId, TTextDocument?> getNewDocument,
                Func<DocumentId, TTextDocument?> getOldDocument)
                where TTextDocument : TextDocument
            {
                foreach (var docId in changedDocuments)
                {
                    var newTextDoc = getNewDocument(docId);
                    var oldTextDoc = getOldDocument(docId);

                    Contract.ThrowIfNull(oldTextDoc);
                    Contract.ThrowIfNull(newTextDoc);

                    var oldText = await oldTextDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    IEnumerable<TextChange> textChanges;

                    // Normal documents have a unique service for calculating minimal text edits. If we used the standard 'GetTextChanges'
                    // method instead, we would get a change that spans the entire document, which we ideally want to avoid.
                    if (newTextDoc is Document newDoc && oldTextDoc is Document oldDoc)
                    {
                        Contract.ThrowIfNull(textDiffService);
                        textChanges = await textDiffService.GetTextChangesAsync(oldDoc, newDoc, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var newText = await newTextDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        textChanges = newText.GetTextChanges(oldText);
                    }

                    var edits = textChanges.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray();
                    var documentIdentifier = new OptionalVersionedTextDocumentIdentifier { Uri = newTextDoc.GetURI() };
                    textDocumentEdits.Add(new TextDocumentEdit { TextDocument = documentIdentifier, Edits = edits });
                }
            }
        }
    }
}
