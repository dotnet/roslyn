// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

internal static class CSharpCompletionItemFormatter
{
    public static async Task<VSInternalCompletionItem> FormatAsync(
        VSInternalCompletionItem resolvedCompletionItem,
        RemoteDocumentContext documentContext,
        Solution solution,
        bool declarationDocument,
        RazorFormattingOptions options,
        IRazorFormattingService formattingService,
        IDocumentMappingService documentMappingService,
        bool supportsVisualStudioExtensions,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument(declarationDocument);

        // In VS Code, Roslyn does resolve via a custom command. Thats fine, but we have to modify the text edit sitting within it,
        // rather than the one LSP knows about.
        if (resolvedCompletionItem.Command is { CommandIdentifier: CompletionResultFactory.CompleteComplexEditCommand, Arguments: var args })
        {
            // In LSP case, command parameters will be JsonElement objects and will need to be deserialized first
            if (args is [JsonElement textDocumentIdentifierData, JsonElement complexEditData, _, _])
            {
                args[0] = textDocumentIdentifierData.Deserialize<TextDocumentIdentifier>() ?? args[0];
                args[1] = complexEditData.Deserialize<TextEdit>() ?? args[1];
            }

            // In cohosting case, command parameters will be of the correct types (or deserialized by now in LSP case)
            if (args is [TextDocumentIdentifier textDocumentIdentifier, TextEdit complexEdit, _, int nextCursorPosition])
            {
                var commandGeneratedDocumentUri = textDocumentIdentifier.DocumentUri.GetRequiredSystemUri();
                // Just in case the edit is for a different document, however unlikely, we'll use the uri as the source of truth
                if (!codeDocument.TryGetCSharpDocumentForGeneratedUri(solution, commandGeneratedDocumentUri, out var commandCSharpDocument))
                {
                    logger.LogError($"Unable to find a generated Razor C# document for URI '{commandGeneratedDocumentUri}'.");
                    resolvedCompletionItem.Command = null;
                    return resolvedCompletionItem;
                }

                var formattedTextEdit = await FormatTextEditsAsync([complexEdit], documentContext, commandCSharpDocument, options, formattingService, cancellationToken).ConfigureAwait(false);
                if (formattedTextEdit is null)
                {
                    resolvedCompletionItem.Command = null;
                }
                else
                {
                    args[0] = new TextDocumentIdentifier()
                    {
                        DocumentUri = documentContext.Uri,
                    };
                    args[1] = formattedTextEdit;
                    if (nextCursorPosition >= 0)
                    {
                        // nextCursorPosition is where VS Code will navigate to, so we translate it to our document, or set to 0 to do nothing.
                        args[3] = documentMappingService.TryMapToRazorDocumentPosition(commandCSharpDocument, nextCursorPosition, out _, out nextCursorPosition)
                            ? nextCursorPosition
                            : 0;
                    }
                }
            }
            else
            {
                logger.LogError($"Unexpected arguments for command '{CompletionResultFactory.CompleteComplexEditCommand}': Expected: [TextDocumentIdentifier, TextEdit, _, int], Actual: {GetArgumentTypesLogString(resolvedCompletionItem)}");
                Debug.Fail("Unexpected arguments for Roslyn complex edit command. Have they changed things?");
            }
        }
        else if (resolvedCompletionItem.Command is not null)
        {
            logger.LogError($"Unsupported command for Razor document: {resolvedCompletionItem.Command.CommandIdentifier}.");
            Debug.Fail("Unexpected command. Do we need to add something to support a new feature?");
        }

        if (resolvedCompletionItem.TextEdit is not null &&
            supportsVisualStudioExtensions &&
            resolvedCompletionItem.VsResolveTextEditOnCommit)
        {
            if (resolvedCompletionItem.TextEdit.Value.TryGetFirst(out var textEdit))
            {
                var formattedTextChange = await FormatTextEditsAsync([textEdit], documentContext, csharpDocument, options, formattingService, cancellationToken).ConfigureAwait(false);
                if (formattedTextChange is not null)
                {
                    resolvedCompletionItem.TextEdit = formattedTextChange;
                }
            }
            else
            {
                // TODO: Handle InsertReplaceEdit type
                // https://github.com/dotnet/razor/issues/8829
                Debug.Fail("Unsupported edit type.");
            }
        }

        if (resolvedCompletionItem.AdditionalTextEdits is not null)
        {
            var formattedTextChange = await FormatTextEditsAsync(resolvedCompletionItem.AdditionalTextEdits, documentContext, csharpDocument, options, formattingService, cancellationToken).ConfigureAwait(false);
            resolvedCompletionItem.AdditionalTextEdits = formattedTextChange is { } change ? [change] : null;
        }

        return resolvedCompletionItem;
    }

    private static string GetArgumentTypesLogString(VSInternalCompletionItem resolvedCompletionItem)
    {
        if (resolvedCompletionItem.Command?.Arguments is { } args)
        {
            return "[" + string.Join(", ", args.Select(a => a.GetType().Name)) + "]";
        }

        return "null";
    }

    private static async Task<TextEdit?> FormatTextEditsAsync(TextEdit[] textEdits, RemoteDocumentContext documentContext, RazorCSharpDocument csharpDocument, RazorFormattingOptions options, IRazorFormattingService formattingService, CancellationToken cancellationToken)
    {
        var sourceText = await documentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);

        var changes = textEdits.SelectAsArray(csharpDocument.Text.GetTextChange);
        var formattedTextChange = await formattingService.TryGetCSharpSnippetFormattingEditAsync(
            documentContext,
            changes,
            csharpDocument.IsDeclarationDocument,
            options,
            cancellationToken).ConfigureAwait(false);

        return formattedTextChange is { } change ? sourceText.GetTextEdit(change) : null;
    }
}
