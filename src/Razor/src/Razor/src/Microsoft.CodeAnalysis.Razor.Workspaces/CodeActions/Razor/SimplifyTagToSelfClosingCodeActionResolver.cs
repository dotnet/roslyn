// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal class SimplifyTagToSelfClosingCodeActionResolver : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing;

    public async Task<WorkspaceEdit?> ResolveAsync(DocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var actionParams = JsonSerializer.Deserialize<SimplifyTagToSelfClosingCodeActionParams>(data.GetRawText());
        if (actionParams is null)
        {
            return null;
        }

        var componentDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var text = componentDocument.Source.Text;
        var removeRange = text.GetRange(actionParams.StartTagCloseAngleIndex, actionParams.EndTagCloseAngleIndex);

        var documentChanges = new TextDocumentEdit[]
        {
            new TextDocumentEdit
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri= new(documentContext.Uri) },
                Edits =
                [
                    new TextEdit
                    {
                        NewText = " />",
                        Range = removeRange,
                    }
                ],
            }
        };

        return new WorkspaceEdit
        {
            DocumentChanges = documentChanges,
        };
    }
}
