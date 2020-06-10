// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.CodeActions
{
    /// <summary>
    /// A codeaction that takes either a LSP command or a LSP codeaction.
    /// If a command is provided, then that is executed on the host side. If a codeaction is
    /// provided then the edits are applied locally on the guest side.
    /// </summary>
    internal class RoslynRemoteCodeAction : CodeAction
    {
        private readonly Document _document;
        private readonly LSP.Command _command;
        private readonly string _title;
        private readonly ILanguageServerClient _lspClient;

        private readonly LSP.Command _codeActionCommand;
        private readonly LSP.WorkspaceEdit _codeActionWorkspaceEdit;

        /// <summary>
        /// Create a remote code action wrapping a command
        /// </summary>
        public RoslynRemoteCodeAction(Document document, LSP.Command command, string title, ILanguageServerClient lspClient)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _lspClient = lspClient ?? throw new ArgumentNullException(nameof(lspClient));
            _command = command ?? throw new ArgumentNullException(nameof(command));
            _title = title;

            _codeActionCommand = null;
            _codeActionWorkspaceEdit = null;
        }

        /// <summary>
        /// Create a remote code action wrapping an LSP code action.
        /// </summary>
        public RoslynRemoteCodeAction(Document document, LSP.Command codeActionCommand, LSP.WorkspaceEdit codeActionWorkspaceEdit, string title, ILanguageServerClient lspClient)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _lspClient = lspClient ?? throw new ArgumentNullException(nameof(lspClient));

            _codeActionCommand = codeActionCommand;
            _codeActionWorkspaceEdit = codeActionWorkspaceEdit ?? throw new ArgumentNullException(nameof(codeActionWorkspaceEdit));
            _title = title;

            _command = null;
        }

        public override string Title => _title;

        protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            // If we have a codeaction, then just call the base method which will call ComputeOperationsAsync below.
            // This creates an ApplyChagesOperation so that Roslyn can show a preview of the changes.
            if (_codeActionWorkspaceEdit != null)
            {
                return await base.ComputePreviewOperationsAsync(cancellationToken).ConfigureAwait(false);
            }

            // We have a command - this will be executed on the host but the host may have a preview for the current document.
            var runCodeActionsCommand = ((JToken)_command.Arguments?.Single()).ToObject<LSP.Command>();
            var runCodeActionParams = ((JToken)runCodeActionsCommand.Arguments?.Single())?.ToObject<RunCodeActionParams>();

            var request = new LspRequest<RunCodeActionParams, LSP.TextEdit[]>(RoslynMethods.CodeActionPreviewName);
            var textEdits = await _lspClient.RequestAsync(request, runCodeActionParams, cancellationToken).ConfigureAwait(false);
            if (textEdits == null || textEdits.Length == 0)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            var newDocument = await ApplyEditsAsync(_document, textEdits, cancellationToken).ConfigureAwait(false);
            return ImmutableArray.Create(new ApplyChangesOperation(newDocument.Project.Solution));
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            // If we have a command, return an operation that will execute the command on the host side.
            if (_command != null)
            {
                var operation = new RoslynRemoteCodeActionOperation(_command, _lspClient);
                return ImmutableArray.Create(operation);
            }

            // We have a codeaction - create an applychanges operation from it.
            var operations = new LinkedList<CodeActionOperation>();
            var newSolution = _document.Project.Solution;
            foreach (var changePair in _codeActionWorkspaceEdit.Changes)
            {
                var documentName = await _lspClient.ProtocolConverter.FromProtocolUriAsync(new Uri(changePair.Key), true, cancellationToken).ConfigureAwait(false);

                // The document name here is a document URI that we're trying to apply an edit to. The edit was already computed on the
                // server and we're trying to create an equivalent copy on the client; we're only fetching the text of the document
                // rather than any syntax trees, and so it doesn't matter from which context we grab if there are linked files --
                // all the files are the same and modifying any of them is fine.
                var doc = newSolution.GetDocuments(documentName).First();
                if (doc == null)
                {
                    continue;
                }

                var newDoc = await ApplyEditsAsync(doc, changePair.Value, cancellationToken).ConfigureAwait(false);
                newSolution = newDoc.Project.Solution;
            }
            operations.AddLast(new ApplyChangesOperation(newSolution));

            if (_codeActionCommand != null)
            {
                operations.AddLast(new RoslynRemoteCodeActionOperation(_codeActionCommand, _lspClient));
            }

            return operations.AsImmutable();
        }

        private async Task<Document> ApplyEditsAsync(Document document, LSP.TextEdit[] textEdits, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textChanges = textEdits.Select(te => new TextChange(ProtocolConversions.RangeToTextSpan(te.Range, text), te.NewText));
            var newDocument = document.WithText(text.WithChanges(textChanges));
            return newDocument;
        }

        protected override Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
        {
            // The solution we have is already post processed.
            return Task.FromResult(document);
        }
    }
}
