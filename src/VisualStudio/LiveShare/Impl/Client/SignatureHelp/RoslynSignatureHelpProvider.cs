// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.SignatureHelp
{
    class RoslynSignatureHelpProvider : ISignatureHelpProvider
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;

        public RoslynSignatureHelpProvider(AbstractLspClientServiceFactory roslynLspClientServiceFactory)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
        }
        public bool IsTriggerCharacter(char ch)
        {
            return ch == '(' || ch == ',';
        }

        public bool IsRetriggerCharacter(char ch)
        {
            return ch == ')';
        }

        public async Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            // This provider is exported for all workspaces - so limit it to just our workspace.
            if (document.Project.Solution.Workspace.Kind != WorkspaceKind.AnyCodeRoslynWorkspace)
            {
                return null;
            }

            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textDocumentPositionParams = ProtocolConversions.PositionToTextDocumentPositionParams(position, text, document);

            var signatureHelp = await lspClient.RequestAsync(Methods.TextDocumentSignatureHelp.ToLSRequest(), textDocumentPositionParams, cancellationToken).ConfigureAwait(false);
            if (signatureHelp == null || signatureHelp.Signatures == null || signatureHelp.Signatures.Length <= 0)
            {
                return null;
            }

            var items = new List<SignatureHelpItem>();
            foreach (var signature in signatureHelp.Signatures)
            {
                items.Add(CreateSignatureHelpItem(signature));
            }

            var linePosition = text.Lines.GetLinePosition(position);
            var applicableSpan = text.Lines.GetTextSpan(new CodeAnalysis.Text.LinePositionSpan(linePosition, linePosition));
            return new SignatureHelpItems(items, applicableSpan, signatureHelp.ActiveParameter, signatureHelp.ActiveParameter, null, signatureHelp.ActiveSignature);
        }

        private SignatureHelpItem CreateSignatureHelpItem(SignatureInformation signatureInformation)
        {
            var signatureText = signatureInformation.Label;
            var emptyText = ToTaggedText(string.Empty);

            var parameters = signatureInformation.Parameters.Select(parameter =>
            {
                Func<CancellationToken, IEnumerable<TaggedText>> paramDocumentationFactory = (ct) => ToTaggedText(parameter.Documentation?.Value);
                return new SignatureHelpParameter((string)parameter.Label, false, paramDocumentationFactory, emptyText);
            });

            return new SignatureHelpItem(false, DocumentationFactory, ToTaggedText(signatureInformation.Label), emptyText, emptyText, parameters, emptyText);

            // local functions
            IEnumerable<TaggedText> DocumentationFactory(CancellationToken ct) => ToTaggedText(signatureInformation.Documentation?.Value);
        }

        private IEnumerable<TaggedText> ToTaggedText(string text)
        {
            return ImmutableArray.Create(new TaggedText(TextTags.Text, text ?? string.Empty));
        }
    }
}
