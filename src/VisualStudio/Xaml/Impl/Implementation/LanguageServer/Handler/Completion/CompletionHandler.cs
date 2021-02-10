﻿// Licensed to the .NET Foundation under one or more agreements.
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
                Items = completionResult.Completions.Select(c => CreateCompletionItem(c, document.Id, text, request.Position)).ToArray(),
                SuggestionMode = false,
            };
        }

        private static CompletionItem CreateCompletionItem(XamlCompletionItem xamlCompletion, DocumentId documentId, SourceText text, Position position)
        {
            var item = new VSCompletionItem
            {
                Label = xamlCompletion.DisplayText,
                CommitCharacters = xamlCompletion.CommitCharacters,
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

            return item;
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
