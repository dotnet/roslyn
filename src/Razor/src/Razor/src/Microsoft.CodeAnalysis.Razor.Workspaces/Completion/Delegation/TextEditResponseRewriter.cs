// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Completion.Delegation;

internal class TextEditResponseRewriter : IDelegatedCSharpCompletionResponseRewriter
{
    public RazorVSInternalCompletionList Rewrite(
        RazorVSInternalCompletionList completionList,
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        Position projectedPosition,
        RazorCompletionOptions completionOptions)
    {
        var sourceText = codeDocument.Source.Text;

        var hostDocumentPosition = sourceText.GetPosition(hostDocumentIndex);
        completionList = TranslateTextEdits(hostDocumentPosition, projectedPosition, completionList);

        if (completionList.ItemDefaults?.EditRange is { } editRange)
        {
            if (editRange.TryGetFirst(out var range))
            {
                completionList.ItemDefaults.EditRange = TranslateRange(hostDocumentPosition, projectedPosition, range);
            }
            else if (editRange.TryGetSecond(out var insertReplaceRange))
            {
                insertReplaceRange.Insert = TranslateRange(hostDocumentPosition, projectedPosition, insertReplaceRange.Insert);
                insertReplaceRange.Replace = TranslateRange(hostDocumentPosition, projectedPosition, insertReplaceRange.Replace);
            }
        }

        return completionList;
    }

    private static RazorVSInternalCompletionList TranslateTextEdits(
        Position hostDocumentPosition,
        Position projectedPosition,
        RazorVSInternalCompletionList completionList)
    {
        // The TextEdit positions returned to us from the C#/HTML language servers are positions correlating to the virtual document.
        // We need to translate these positions to apply to the Razor document instead. Performance is a big concern here, so we want to
        // make the logic as simple as possible, i.e. no asynchronous calls.
        // The current logic takes the approach of assuming the original request's position (Razor doc) correlates directly to the positions
        // returned by the C#/HTML language servers. We use this assumption (+ math) to map from the virtual (projected) doc positions ->
        // Razor doc positions.

        foreach (var item in completionList.Items)
        {
            if (item.TextEdit is { } edit)
            {
                if (edit.TryGetFirst(out var textEdit))
                {
                    var translatedRange = TranslateRange(hostDocumentPosition, projectedPosition, textEdit.Range);
                    textEdit.Range = translatedRange;
                }
                else if (edit.TryGetSecond(out var insertReplaceEdit))
                {
                    insertReplaceEdit.Insert = TranslateRange(hostDocumentPosition, projectedPosition, insertReplaceEdit.Insert);
                    insertReplaceEdit.Replace = TranslateRange(hostDocumentPosition, projectedPosition, insertReplaceEdit.Replace);
                }
            }
            else if (item.AdditionalTextEdits is not null)
            {
                // Additional text edits should typically only be provided at resolve time. We don't support them in the normal completion flow.
                item.AdditionalTextEdits = null;
            }
        }

        return completionList;
    }

    private static LspRange TranslateRange(Position hostDocumentPosition, Position projectedPosition, LspRange textEditRange)
    {
        var offset = projectedPosition.Character - hostDocumentPosition.Character;

        var translatedStartPosition = TranslatePosition(offset, hostDocumentPosition, textEditRange.Start);
        var translatedEndPosition = TranslatePosition(offset, hostDocumentPosition, textEditRange.End);

        return LspFactory.CreateRange(translatedStartPosition, translatedEndPosition);

        static Position TranslatePosition(int offset, Position hostDocumentPosition, Position editPosition)
        {
            var translatedCharacter = editPosition.Character - offset;

            // Note: If this completion handler ever expands to deal with multi-line TextEdits, this logic will likely need to change since
            // it assumes we're only dealing with single-line TextEdits.
            return LspFactory.CreatePosition(hostDocumentPosition.Line, translatedCharacter);
        }
    }
}
