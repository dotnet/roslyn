// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    [ExportLspRequestHandlerProvider(StringConstants.XamlLanguageName), Shared]
    [ProvidesMethod(Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : AbstractStatelessRequestHandler<CompletionParams, CompletionList?>
    {
        public override string Method => Methods.TextDocumentCompletionName;
        private const string CreateEventHandlerCommandTitle = "Create Event Handler";
        private const string RetriggerCompletionCommandTitle = "Re-trigger completions";

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler()
        {
        }

        public override TextDocumentIdentifier GetTextDocumentIdentifier(CompletionParams request) => request.TextDocument;

        public override async Task<CompletionList?> HandleRequestAsync(CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            if (request.Context is VSCompletionContext completionContext)
            {
                if (completionContext.InvokeKind == VSCompletionInvokeKind.Deletion)
                {
                    // Don't trigger completions on backspace.
                    return null;
                }
            }

            var document = context.Document;
            if (document == null)
            {
                return null;
            }

            var completionService = document.Project.LanguageServices.GetRequiredService<IXamlCompletionService>();
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));
            var completionResult = await completionService.GetCompletionsAsync(new XamlCompletionContext(document, offset, request.Context?.TriggerCharacter?.FirstOrDefault() ?? '\0'), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (completionResult?.Completions == null)
            {
                return null;
            }

            return new VSCompletionList
            {
                Items = completionResult.Completions.Select(c => CreateCompletionItem(c, document.Id, text, request.Position, request.TextDocument)).ToArray(),
                SuggestionMode = false,
            };
        }

        private static CompletionItem CreateCompletionItem(XamlCompletionItem xamlCompletion, DocumentId documentId, SourceText text, Position position, TextDocumentIdentifier textDocument)
        {
            var item = new VSCompletionItem
            {
                Label = xamlCompletion.DisplayText,
                VsCommitCharacters = GetCommitCharacters(xamlCompletion),
                Detail = xamlCompletion.Detail,
                InsertText = xamlCompletion.InsertText,
                Preselect = xamlCompletion.Preselect.GetValueOrDefault(),
                SortText = xamlCompletion.SortText,
                FilterText = xamlCompletion.FilterText,
                Kind = GetItemKind(xamlCompletion.Kind),
                Description = xamlCompletion.Description,
                Icon = xamlCompletion.Icon,
                Data = new CompletionResolveData { ProjectGuid = documentId.ProjectId.Id, DocumentGuid = documentId.Id, Position = position, DisplayText = xamlCompletion.DisplayText }
            };

            if (xamlCompletion.Span.HasValue)
            {
                item.TextEdit = new TextEdit
                {
                    NewText = xamlCompletion.InsertText,
                    Range = ProtocolConversions.LinePositionToRange(text.Lines.GetLinePositionSpan(xamlCompletion.Span.Value))
                };
            }

            if (xamlCompletion.EventDescription.HasValue)
            {
                item.Command = new Command()
                {
                    CommandIdentifier = StringConstants.CreateEventHandlerCommand,
                    Arguments = new object[] { textDocument, xamlCompletion.EventDescription },
                    Title = CreateEventHandlerCommandTitle
                };
            }
            else if (xamlCompletion.RetriggerCompletion)
            {
                // Retriger completion after commit
                item.Command = new Command()
                {
                    CommandIdentifier = StringConstants.RetriggerCompletionCommand,
                    Title = RetriggerCompletionCommandTitle
                };
            }

            return item;
        }

        private static SumType<string[], CommitCharacter[]> GetCommitCharacters(XamlCompletionItem completionItem)
        {
            if(completionItem.XamlCommitCharacters == null)
            {
                return completionItem.CommitCharacters;
            }

            return completionItem.XamlCommitCharacters.Select(c => new CommitCharacter() { Character = c.Character.ToString(), Insert = c.Insert }).ToArray();
        }

        private static CompletionItemKind GetItemKind(XamlCompletionKind kind)
        {
            switch (kind)
            {
                case XamlCompletionKind.Element:
                case XamlCompletionKind.ElementName:
                    return CompletionItemKind.Element;
                case XamlCompletionKind.EndTag:
                    return CompletionItemKind.CloseElement;
                case XamlCompletionKind.Attribute:
                case XamlCompletionKind.AttachedPropertyValue:
                case XamlCompletionKind.ConditionalArgument:
                case XamlCompletionKind.DataBoundProperty:
                case XamlCompletionKind.MarkupExtensionParameter:
                case XamlCompletionKind.PropertyElement:
                    return CompletionItemKind.Property;
                case XamlCompletionKind.ConditionValue:
                case XamlCompletionKind.MarkupExtensionValue:
                case XamlCompletionKind.PropertyValue:
                case XamlCompletionKind.Value:
                    return CompletionItemKind.Value;
                case XamlCompletionKind.Event:
                case XamlCompletionKind.EventHandlerDescription:
                    return CompletionItemKind.Event;
                case XamlCompletionKind.NamespaceValue:
                case XamlCompletionKind.Prefix:
                    return CompletionItemKind.Namespace;
                case XamlCompletionKind.AttachedPropertyTypePrefix:
                case XamlCompletionKind.MarkupExtensionClass:
                case XamlCompletionKind.Type:
                case XamlCompletionKind.TypePrefix:
                    return CompletionItemKind.Class;
                case XamlCompletionKind.LocalResource:
                    return CompletionItemKind.LocalResource;
                case XamlCompletionKind.SystemResource:
                    return CompletionItemKind.SystemResource;
                case XamlCompletionKind.CData:
                case XamlCompletionKind.Comment:
                case XamlCompletionKind.ProcessingInstruction:
                case XamlCompletionKind.RegionStart:
                case XamlCompletionKind.RegionEnd:
                    return CompletionItemKind.Keyword;
                case XamlCompletionKind.Snippet:
                    return CompletionItemKind.Snippet;
                default:
                    Debug.Fail($"Unhandled {nameof(XamlCompletionKind)}: {Enum.GetName(typeof(XamlCompletionKind), kind)}");
                    return CompletionItemKind.Text;
            }
        }
    }
}
