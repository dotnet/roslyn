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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    [Shared]
    [ExportLspMethod(Methods.TextDocumentCompletionName, StringConstants.XamlLanguageName)]
    internal class CompletionHandler : AbstractRequestHandler<CompletionParams, CompletionItem[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<CompletionItem[]> HandleRequestAsync(CompletionParams request, ClientCapabilities clientCapabilities, string clientName, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetTextDocument(request.TextDocument, clientName);
            if (document == null)
            {
                return CreateErrorItem($"Cannot find document in solution!", request.TextDocument.Uri.ToString());
            }

            var completionService = document.Project.LanguageServices.GetRequiredService<IXamlCompletionService>();
            var offset = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var completions = await completionService.GetCompletionsAsync(document, offset, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (completions == null)
            {
                return Array.Empty<CompletionItem>();
            }

            return completions.Select(c => CreateCompletionItem(c, document.Id, request.Position)).ToArray();
        }

        private static CompletionItem CreateCompletionItem(IXamlCompletionItem xamlCompletion, DocumentId documentId, Position position)
            => new VSCompletionItem
            {
                Label = xamlCompletion.DisplayText,
                CommitCharacters = xamlCompletion.CommitCharacters,
                Detail = xamlCompletion.Detail,
                InsertText = xamlCompletion.InsertText,
                Preselect = xamlCompletion.Preselect,
                SortText = xamlCompletion.SortText,
                FilterText = xamlCompletion.FilterText,
                Kind = GetItemKind(xamlCompletion.Kind),
                Description = xamlCompletion.Description,
                Icon = xamlCompletion.Icon,
                Data = new CompletionResolveData { ProjectGuid = documentId.ProjectId.Id, DocumentGuid = documentId.Id, Position = position, DisplayText = xamlCompletion.DisplayText }
            };

        private static CompletionItem[] CreateErrorItem(string message, string details = null)
        {
            var item = new CompletionItem
            {
                Label = message,
                Documentation = details,
                InsertText = string.Empty,
                Kind = CompletionItemKind.Text,
            };

            return new[] { item };
        }

        private static CompletionItemKind GetItemKind(XamlCompletionKind kind)
        {
            switch (kind)
            {
                case XamlCompletionKind.Element:
                case XamlCompletionKind.ElementName:
                case XamlCompletionKind.EndTag:
                    return CompletionItemKind.Class;
                case XamlCompletionKind.Attribute:
                case XamlCompletionKind.AttachedPropertyValue:
                case XamlCompletionKind.PropertyElement:
                case XamlCompletionKind.MarkupExtensionParameter:
                case XamlCompletionKind.ConditionalArgument:
                    return CompletionItemKind.Property;
                case XamlCompletionKind.MarkupExtensionValue:
                case XamlCompletionKind.PropertyValue:
                case XamlCompletionKind.NamespaceValue:
                case XamlCompletionKind.ConditionValue:
                case XamlCompletionKind.Value:
                    return CompletionItemKind.Value;
                case XamlCompletionKind.Event:
                case XamlCompletionKind.EventHandlerDescription:
                    return CompletionItemKind.Event;
                case XamlCompletionKind.MarkupExtensionClass:
                    return CompletionItemKind.Method;
                case XamlCompletionKind.Prefix:
                    return CompletionItemKind.Constant;
                case XamlCompletionKind.Type:
                case XamlCompletionKind.TypePrefix:
                case XamlCompletionKind.AttachedPropertyTypePrefix:
                    return CompletionItemKind.TypeParameter;
                case XamlCompletionKind.LocalResource:
                    return CompletionItemKind.Reference;
                case XamlCompletionKind.SystemResource:
                    return CompletionItemKind.Reference;
                case XamlCompletionKind.CData:
                case XamlCompletionKind.Comment:
                case XamlCompletionKind.ProcessingInstruction:
                case XamlCompletionKind.RegionStart:
                case XamlCompletionKind.RegionEnd:
                    return CompletionItemKind.Keyword;
                case XamlCompletionKind.DataBoundProperty:
                    return CompletionItemKind.Variable;
                case XamlCompletionKind.Snippet:
                    return CompletionItemKind.Snippet;
                default:
                    Debug.Fail($"Unhandled {nameof(XamlCompletionKind)}: {Enum.GetName(typeof(XamlCompletionKind), kind)}");
                    return CompletionItemKind.Text;
            }
        }
    }
}
