//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Cascade.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    class RoslynSignatureHelpProvider : ISignatureHelpProvider
    {
        private readonly RoslynLSPClientServiceFactory roslynLSPClientServiceFactory;
        private readonly IVsConfigurationSettings configurationSettings;

        public RoslynSignatureHelpProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory,
            IVsConfigurationSettings configurationSettings)
        {
            this.roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
            this.configurationSettings = configurationSettings ?? throw new ArgumentNullException(nameof(configurationSettings));
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

            var lspClient = this.roslynLSPClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textDocumentPositionParams = document.GetTextDocumentPositionParams(text, position);

            SignatureHelp signatureHelp = await lspClient.RequestAsync(Methods.TextDocumentSignatureHelp, textDocumentPositionParams, cancellationToken).ConfigureAwait(false);
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

            IEnumerable<SignatureHelpParameter> parameters = signatureInformation.Parameters.Select(parameter =>
            {
                Func<CancellationToken, IEnumerable<TaggedText>> paramDocumentationFactory = (ct) => ToTaggedText(parameter.Documentation?.Value);
                return new SignatureHelpParameter((string)parameter.Label, false, paramDocumentationFactory, emptyText);
            });

            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory = (ct) => ToTaggedText(signatureInformation.Documentation?.Value);
            return new SignatureHelpItem(false, documentationFactory, ToTaggedText(signatureInformation.Label), emptyText, emptyText, parameters, emptyText);
        }
        
        private IEnumerable<TaggedText> ToTaggedText(string text)
        {
            return ImmutableArray.Create(new TaggedText(TextTags.Text, text ?? string.Empty));
        }
    }
}
