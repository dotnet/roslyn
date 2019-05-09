// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    /// <summary>
    /// A codeaction that takes either a LSP command or a LSP codeaction.
    /// If a command is provided, then that is executed on the host side. If a codeaction is
    /// provided then the edits are applied locally on the guest side.
    /// </summary>
    internal class RoslynRemoteCodeAction : CodeAnalysis.CodeActions.CodeAction
    {
        private readonly Document _document;
        private readonly LSP.Command _command;
        private readonly LSP.CodeAction _lspCodeAction;
        private readonly ILanguageServerClient _lspClient;

        public RoslynRemoteCodeAction(Document document, LSP.Command command, LSP.CodeAction lspCodeAction, ILanguageServerClient lspClient)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _lspClient = lspClient ?? throw new ArgumentNullException(nameof(lspClient));

            _command = command;
            _lspCodeAction = lspCodeAction;
            if (!(_command == null ^ _lspCodeAction == null))
            {
                throw new ArgumentException("Either command or codeaction and only one of those should be provided");
            }
        }

        public override string Title => _command?.Title ?? _lspCodeAction.Title;

        protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            // If we have a codeaction, then just call the base method which will call ComputeOperationsAsync below.
            // This creates an ApplyChagesOperation so that Roslyn can show a preview of the changes.
            if (_lspCodeAction != null)
            {
                return await base.ComputePreviewOperationsAsync(cancellationToken).ConfigureAwait(false);
            }

            // We have a command - this will be executed on the host but the host may have a preview for the current document.
            var runCodeActionParams = ((JToken)_command.Arguments?.Single())?.ToObject<RunCodeActionParams>();

            var request = new LSP.LspRequest<RunCodeActionParams, LSP.TextEdit[]>(RoslynMethods.CodeActionPreviewName);
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
            foreach (var changePair in _lspCodeAction.Edit.Changes)
            {
                var documentName = await _lspClient.ProtocolConverter.FromProtocolUriAsync(new Uri(changePair.Key), true, cancellationToken).ConfigureAwait(false);
                var doc = newSolution.GetDocumentFromURI(documentName);
                if (doc == null)
                {
                    continue;
                }

                var newDoc = await ApplyEditsAsync(doc, changePair.Value, cancellationToken).ConfigureAwait(false);
                newSolution = newDoc.Project.Solution;
            }
            operations.AddLast(new ApplyChangesOperation(newSolution));

            if (_lspCodeAction.Command != null)
            {
                operations.AddLast(new RoslynRemoteCodeActionOperation(_lspCodeAction.Command, _lspClient));
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
