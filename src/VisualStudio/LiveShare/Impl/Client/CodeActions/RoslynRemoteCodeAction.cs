//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Remote.Shared.CustomProtocol;
using Newtonsoft.Json.Linq;
using LspCodeAction = Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol.CodeAction;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    /// <summary>
    /// A codeaction that takes either a LSP command or a LSP codeaction.
    /// If a command is provided, then that is executed on the host side. If a codeaction is
    /// provided then the edits are applied locally on the guest side.
    /// </summary>
    internal class RoslynRemoteCodeAction : CodeAnalysis.CodeActions.CodeAction
    {
        private readonly Document document;
        private readonly Command command;
        private readonly LspCodeAction lspCodeAction;
        private readonly ILanguageServerClient lspClient;

        public RoslynRemoteCodeAction(Document document, Command command, LspCodeAction lspCodeAction, ILanguageServerClient lspClient)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));
            this.lspClient = lspClient ?? throw new ArgumentNullException(nameof(lspClient));

            this.command = command;
            this.lspCodeAction = lspCodeAction;
            if (!(this.command == null ^ this.lspCodeAction == null))
            {
                throw new ArgumentException("Either command or codeaction and only one of those should be provided");
            }
        }

        public override string Title => this.command?.Title ?? this.lspCodeAction.Title;

        protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            // If we have a codeaction, then just call the base method which will call ComputeOperationsAsync below.
            // This creates an ApplyChagesOperation so that Roslyn can show a preview of the changes.
            if (this.lspCodeAction != null)
            {
                return await base.ComputePreviewOperationsAsync(cancellationToken).ConfigureAwait(false);
            }

            // We have a command - this will be executed on the host but the host may have a preview for the current document.
            var runCodeActionParams = ((JToken)this.command.Arguments?.Single())?.ToObject<RunCodeActionParams>();

            TextEdit[] textEdits = await this.lspClient.RequestAsync(RoslynMethods.CodeActionPreview, runCodeActionParams, cancellationToken).ConfigureAwait(false);
            if (textEdits == null || textEdits.Length == 0)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            Document newDocument = await ApplyEditsAsync(this.document, textEdits, cancellationToken).ConfigureAwait(false);
            return ImmutableArray.Create(new ApplyChangesOperation(newDocument.Project.Solution));
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            // If we have a command, return an operation that will execute the command on the host side.
            if (this.command != null)
            {
                var operation = new RoslynRemoteCodeActionOperation(this.command, this.lspClient);
                return ImmutableArray.Create(operation);
            }

            // We have a codeaction - create an applychanges operation from it.
            var operations = new LinkedList<CodeActionOperation>();
            var newSolution = this.document.Project.Solution;
            foreach (var changePair in this.lspCodeAction.Edit.Changes)
            {
                var documentName = await this.lspClient.ProtocolConverter.FromProtocolUriAsync(new Uri(changePair.Key), true, cancellationToken).ConfigureAwait(false);
                var doc = newSolution.GetDocument(documentName.LocalPath);
                if (doc == null)
                {
                    continue;
                }

                var newDoc = await ApplyEditsAsync(doc, changePair.Value, cancellationToken).ConfigureAwait(false);
                newSolution = newDoc.Project.Solution;
            }
            operations.AddLast(new ApplyChangesOperation(newSolution));

            if (this.lspCodeAction.Command != null)
            {
                operations.AddLast(new RoslynRemoteCodeActionOperation(this.lspCodeAction.Command, this.lspClient));
            }

            return operations.AsImmutable();
        }

        private async Task<Document> ApplyEditsAsync(Document document, TextEdit[] textEdits, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            IEnumerable<TextChange> textChanges = textEdits.Select(te => new TextChange(te.Range.ToTextSpan(text), te.NewText));
            Document newDocument = document.WithText(text.WithChanges(textChanges));
            return newDocument;
        }

        protected override Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
        {
            // The solution we have is already post processed.
            return Task.FromResult(document);
        }
    }
}
