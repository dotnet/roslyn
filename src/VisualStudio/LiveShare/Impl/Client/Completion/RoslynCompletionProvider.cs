// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Completion
{
    internal class RoslynCompletionProvider : CommonCompletionProvider
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;

        public RoslynCompletionProvider(AbstractLspClientServiceFactory roslynLspClientServiceFactory)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            // This provider is exported for all workspaces - so limit it to just our workspace & the debugger's intellisense workspace
            if (context.Document.Project.Solution.Workspace.Kind != WorkspaceKind.AnyCodeRoslynWorkspace &&
                context.Document.Project.Solution.Workspace.Kind != StringConstants.DebuggerIntellisenseWorkspaceKind)
            {
                return;
            }

            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

            var completionParams = new LSP.CompletionParams
            {
                TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(context.Document),
                Position = ProtocolConversions.LinePositionToPosition(text.Lines.GetLinePosition(context.Position)),
                Context = new LSP.CompletionContext { TriggerCharacter = context.Trigger.Character.ToString(), TriggerKind = GetTriggerKind(context.Trigger) }
            };

            var completionObject = await lspClient.RequestAsync(LSP.Methods.TextDocumentCompletion.ToLSRequest(), completionParams, context.CancellationToken).ConfigureAwait(false);
            if (completionObject == null)
            {
                return;
            }

            var completionList = ((JToken)completionObject).ToObject<RoslynCompletionItem[]>();

            foreach (var item in completionList)
            {
                ImmutableArray<string> tags;
                if (item.Tags != null)
                {
                    tags = item.Tags.AsImmutable();
                }
                else
                {
                    var glyph = ProtocolConversions.CompletionItemKindToGlyph(item.Kind);
                    tags = GlyphTags.GetTags(glyph);
                }

                var properties = ImmutableDictionary.CreateBuilder<string, string>();
                if (!string.IsNullOrEmpty(item.Detail))
                {
                    // The display text is encoded as TaggedText | value
                    properties.Add("Description", $"Text|{item.Detail}");
                }

                properties.Add("InsertionText", item.InsertText);
                properties.Add("ResolveData", JToken.FromObject(item).ToString());
                var completionItem = CompletionItem.Create(item.Label, item.FilterText, item.SortText, properties: properties.ToImmutable(), tags: tags);
                context.AddItem(completionItem);
            }
        }

        protected override async Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
            }

            if (!item.Properties.TryGetValue("ResolveData", out var serializedItem))
            {
                return await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
            }

            var completionItem = JToken.Parse(serializedItem).ToObject<RoslynCompletionItem>();
            var request = new LspRequest<RoslynCompletionItem, RoslynCompletionItem>(LSP.Methods.TextDocumentCompletionResolveName);
            var resolvedCompletionItem = await lspClient.RequestAsync(request, completionItem, cancellationToken).ConfigureAwait(false);
            if (resolvedCompletionItem?.Description == null)
            {
                return await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
            }

            var parts = resolvedCompletionItem.Description.Select(tt => tt.ToTaggedText()).AsImmutable();
            return CompletionDescription.Create(parts);
        }

        private LSP.CompletionTriggerKind GetTriggerKind(CompletionTrigger trigger)
        {
            if (trigger.Kind == CompletionTriggerKind.Insertion || trigger.Kind == CompletionTriggerKind.Deletion)
            {
                return LSP.CompletionTriggerKind.TriggerCharacter;
            }

            return LSP.CompletionTriggerKind.Invoked;
        }

        public override Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            selectedItem.Properties.TryGetValue("InsertionText", out var text);
            if (text != null)
            {
                return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, text));
            }

            return Task.FromResult<TextChange?>(null);
        }
    }
}
