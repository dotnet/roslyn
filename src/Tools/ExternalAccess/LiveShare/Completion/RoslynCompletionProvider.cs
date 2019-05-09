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
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynCompletionProvider : CommonCompletionProvider
    {
        private readonly RoslynLSPClientServiceFactory _roslynLSPClientServiceFactory;

        public RoslynCompletionProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
        {
            _roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
        }

        public override async Task ProvideCompletionsAsync(CodeAnalysis.Completion.CompletionContext context)
        {
            // This provider is exported for all workspaces - so limit it to just our workspace & the debugger's intellisense workspace
            if (context.Document.Project.Solution.Workspace.Kind != WorkspaceKind.AnyCodeRoslynWorkspace &&
                context.Document.Project.Solution.Workspace.Kind != StringConstants.DebuggerIntellisenseWorkspaceKind)
            {
                return;
            }

            var lspClient = _roslynLSPClientServiceFactory.ActiveLanguageServerClient;
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

            var completionObject = await lspClient.RequestAsync(LSP.Methods.TextDocumentCompletion, completionParams, context.CancellationToken).ConfigureAwait(false);
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
                var completionItem = CodeAnalysis.Completion.CompletionItem.Create(item.Label, item.FilterText, item.SortText, properties: properties.ToImmutable(), tags: tags);
                context.AddItem(completionItem);
            }
        }

        protected override async Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CodeAnalysis.Completion.CompletionItem item, CancellationToken cancellationToken)
        {
            var lspClient = _roslynLSPClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
            }

            if (!item.Properties.TryGetValue("ResolveData", out string serializedItem))
            {
                return await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
            }

            var completionItem = JToken.Parse(serializedItem).ToObject<RoslynCompletionItem>();
            var request = new LSP.LspRequest<RoslynCompletionItem, RoslynCompletionItem>(LSP.Methods.TextDocumentCompletionResolveName);
            var resolvedCompletionItem = await lspClient.RequestAsync(request, completionItem, cancellationToken).ConfigureAwait(false);
            if (resolvedCompletionItem?.Description == null)
            {
                return await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
            }

            var parts = resolvedCompletionItem.Description.Runs.Select(run => new TaggedText(run.ClassificationTypeName, run.Text)).AsImmutable();
            return CompletionDescription.Create(parts);
        }

        private LSP.CompletionTriggerKind GetTriggerKind(CompletionTrigger trigger)
        {
            if (trigger.Kind == CodeAnalysis.Completion.CompletionTriggerKind.Insertion || trigger.Kind == CodeAnalysis.Completion.CompletionTriggerKind.Deletion)
            {
                return LSP.CompletionTriggerKind.TriggerCharacter;
            }

            return LSP.CompletionTriggerKind.Invoked;
        }

        public override Task<TextChange?> GetTextChangeAsync(Document document, CodeAnalysis.Completion.CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
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
