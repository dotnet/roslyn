// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Handle a completion request.
/// </summary>
[ExportXamlStatelessLspService(typeof(CompletionHandler)), Shared]
[XamlMethod(Methods.TextDocumentCompletionName)]
internal class CompletionHandler : ILspServiceDocumentRequestHandler<CompletionParams, CompletionList?>
{
    private const string CreateEventHandlerCommandTitle = "Create Event Handler";

    private static readonly Command s_retriggerCompletionCommand = new Command()
    {
        CommandIdentifier = StringConstants.RetriggerCompletionCommand,
        Title = "Re-trigger completions"
    };

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CompletionHandler()
    {
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(CompletionParams request) => request.TextDocument;

    public async Task<CompletionList?> HandleRequestAsync(CompletionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        if (request.Context is VSInternalCompletionContext completionContext && completionContext.InvokeKind == VSInternalCompletionInvokeKind.Deletion)
        {
            // Don't trigger completions on backspace.
            return null;
        }

        var document = context.TextDocument;
        if (document is null)
        {
            return null;
        }

        var completionService = document.Project.Services.GetService<IXamlCompletionService>();
        if (completionService is null)
        {
            return null;
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));
        var completionResult = await completionService.GetCompletionsAsync(new XamlCompletionContext(document, offset, request.Context?.TriggerCharacter?.FirstOrDefault() ?? '\0'), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (completionResult?.Completions is null)
        {
            return null;
        }

        var documentCache = context.GetRequiredLspService<DocumentCache>();
        var documentId = documentCache.UpdateCache(request.TextDocument);
        var commitCharactersCache = new Dictionary<XamlCompletionKind, ImmutableArray<VSInternalCommitCharacter>>();
        return new VSInternalCompletionList
        {
            Items = completionResult.Completions.Select(c => CreateCompletionItem(c, text, request.Position, documentId, commitCharactersCache)).ToArray(),
            SuggestionMode = false,
        };
    }

    private static CompletionItem CreateCompletionItem(XamlCompletionItem xamlCompletion, SourceText text, Position position, long documentId, Dictionary<XamlCompletionKind, ImmutableArray<VSInternalCommitCharacter>> commitCharactersCache)
    {
        var item = new VSInternalCompletionItem
        {
            Label = xamlCompletion.DisplayText,
            VsCommitCharacters = GetCommitCharacters(xamlCompletion, commitCharactersCache),
            Detail = xamlCompletion.Detail,
            InsertText = xamlCompletion.InsertText,
            Preselect = xamlCompletion.Preselect ?? false,
            SortText = xamlCompletion.SortText,
            FilterText = xamlCompletion.FilterText,
            Kind = GetItemKind(xamlCompletion.Kind),
            Description = xamlCompletion.Description,
            Icon = xamlCompletion.Icon,
            InsertTextFormat = xamlCompletion.IsSnippet == true ? InsertTextFormat.Snippet : InsertTextFormat.Plaintext,
            Data = new CompletionResolveData(position, documentId)
        };

        if (xamlCompletion.Span.HasValue)
        {
            item.TextEdit = new TextEdit
            {
                NewText = xamlCompletion.InsertText ?? xamlCompletion.DisplayText,
                Range = ProtocolConversions.LinePositionToRange(text.Lines.GetLinePositionSpan(xamlCompletion.Span.Value))
            };
        }

        if (xamlCompletion.EventDescription is not null)
        {
            item.Command = new Command()
            {
                CommandIdentifier = StringConstants.CreateEventHandlerCommand,
                Arguments = new object[] { xamlCompletion.EventDescription },
                Title = CreateEventHandlerCommandTitle
            };
        }
        else if (xamlCompletion.RetriggerCompletion == true)
        {
            // Retrigger completion after commit
            item.Command = s_retriggerCompletionCommand;
        }

        return item;
    }

    private static SumType<string[], VSInternalCommitCharacter[]>? GetCommitCharacters(XamlCompletionItem completionItem, Dictionary<XamlCompletionKind, ImmutableArray<VSInternalCommitCharacter>> commitCharactersCache)
    {
        if (completionItem.XamlCommitCharacters is null)
        {
            return null;
        }

        if (commitCharactersCache.TryGetValue(completionItem.Kind, out var cachedCharacters))
        {
            // If we have already cached the commit characters, return the cached ones
            return cachedCharacters.ToArray();
        }

        var xamlCommitCharacters = completionItem.XamlCommitCharacters;

        var commitCharacters = xamlCommitCharacters.Characters.Select(c => new VSInternalCommitCharacter { Character = c.ToString(), Insert = !xamlCommitCharacters.NonInsertCharacters.Contains(c) }).ToImmutableArray();
        commitCharactersCache.Add(completionItem.Kind, commitCharacters);
        return commitCharacters.ToArray();
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
