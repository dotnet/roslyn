// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.AutoInsert;

internal class CloseTextTagOnAutoInsertProvider : IOnAutoInsertProvider
{
    public string TriggerCharacter => ">";

    public bool TryResolveInsertion(
        Position position,
        RazorCodeDocument codeDocument,
        bool enableAutoClosingTags,
        [NotNullWhen(true)] out VSInternalDocumentOnAutoInsertResponseItem? autoInsertEdit)
    {
        if (!enableAutoClosingTags || !IsAtTextTag(codeDocument, position))
        {
            autoInsertEdit = null;
            return false;
        }

        // This is a text tag.
        var format = InsertTextFormat.Snippet;
        var edit = LspFactory.CreateTextEdit(position, $"$0</{SyntaxConstants.TextTagName}>");

        autoInsertEdit = new()
        {
            TextEdit = edit,
            TextEditFormat = format
        };

        return true;
    }

    private static bool IsAtTextTag(RazorCodeDocument codeDocument, Position position)
    {
        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var absoluteIndex))
        {
            return false;
        }

        var syntaxRoot = codeDocument.GetRequiredSyntaxRoot();
        var token = syntaxRoot.FindToken(absoluteIndex - 1);

        // Make sure the end </text> tag doesn't already exist
        if (token.Parent is MarkupStartTagSyntax
            {
                IsMarkupTransition: true,
                Parent: MarkupElementSyntax { EndTag: null }
            } startTag)
        {
            Debug.Assert(startTag.Name.Content == SyntaxConstants.TextTagName, "MarkupTransition that is not a <text> tag.");

            return true;
        }

        return false;
    }
}
