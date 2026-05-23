// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

// We want to generate a HTML document that contains only pure HTML.
// So we want replace all non-HTML content with whitespace.
// Ideally we should just use ClassifiedSpans to generate this document but
// not all characters in the document are included in the ClassifiedSpans.
internal sealed class RazorHtmlWriter : SyntaxWalker
{
    private static ReadOnlyMemory<char> Tildes => "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~".AsMemory(); // 32 '~' characters
    private static ReadOnlyMemory<char> CommentStart => "/*".AsMemory();
    private static ReadOnlyMemory<char> CommentEnd => "*/".AsMemory();

    private readonly RazorSourceDocument _source;
    private readonly CodeWriter _codeWriter;

    private bool _isWritingHtml;

    // Rather than writing out C# characters as we find them (as '~') we keep a count so that consecutive characters
    // can be written as a block, allowing any block of 4 characters or more to be written as a comment (ie '/**/`)
    // which takes pressure off the TypeScript/JavaScript compiler. Doing this per token means we can end up with
    // "@className" being written as '~/*~~~~~*/', which means Html formatting will insert a space which breaks things.
    private int _placeholderSize;

    private RazorHtmlWriter(RazorSourceDocument source, CodeWriter codeWriter)
    {
        _source = source;
        _codeWriter = codeWriter;
        _isWritingHtml = true;
    }

    public static RazorHtmlDocument GetHtmlDocument(RazorCodeDocument codeDocument)
    {
        var source = codeDocument.Source;
        var options = codeDocument.CodeGenerationOptions;

        using var codeWriter = new CodeWriter(options);

        var htmlWriter = new RazorHtmlWriter(source, codeWriter);
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();

        htmlWriter.Visit(syntaxTree);

        var text = codeWriter.GetText();

        Debug.Assert(
            source.Text.Length == text.Length,
            $"The backing HTML document should be the same length as the original document. Expected: {source.Text.Length} Actual: {text.Length}");

        return new RazorHtmlDocument(text);
    }

    private void Visit(RazorSyntaxTree syntaxTree)
    {
        Visit(syntaxTree.Root);

        WriteCSharpContentPlaceholder();
    }

    public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
    {
        using (NonHtmlScope())
        {
            base.VisitRazorCommentBlock(node);
        }
    }

    public override void VisitRazorMetaCode(RazorMetaCodeSyntax node)
    {
        using (NonHtmlScope())
        {
            base.VisitRazorMetaCode(node);
        }
    }

    public override void VisitMarkupTransition(MarkupTransitionSyntax node)
    {
        using (NonHtmlScope())
        {
            base.VisitMarkupTransition(node);
        }
    }

    public override void VisitCSharpTransition(CSharpTransitionSyntax node)
    {
        using (NonHtmlScope())
        {
            base.VisitCSharpTransition(node);
        }
    }

    public override void VisitCSharpEphemeralTextLiteral(CSharpEphemeralTextLiteralSyntax node)
    {
        using (NonHtmlScope())
        {
            base.VisitCSharpEphemeralTextLiteral(node);
        }
    }

    public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
    {
        using (NonHtmlScope())
        {
            base.VisitCSharpExpressionLiteral(node);
        }
    }

    public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
    {
        using (NonHtmlScope())
        {
            base.VisitCSharpStatementLiteral(node);
        }
    }

    public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
    {
        using (HtmlScope())
        {
            base.VisitMarkupStartTag(node);
        }
    }

    public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
    {
        using (HtmlScope())
        {
            base.VisitMarkupEndTag(node);
        }
    }

    public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
    {
        using (HtmlScope())
        {
            base.VisitMarkupTagHelperStartTag(node);
        }
    }

    public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
    {
        using (HtmlScope())
        {
            base.VisitMarkupTagHelperEndTag(node);
        }
    }

    public override void VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
    {
        using (HtmlScope())
        {
            base.VisitMarkupEphemeralTextLiteral(node);
        }
    }

    public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
    {
        using (HtmlScope())
        {
            base.VisitMarkupTextLiteral(node);
        }
    }

    public override void VisitUnclassifiedTextLiteral(UnclassifiedTextLiteralSyntax node)
    {
        using (HtmlScope())
        {
            base.VisitUnclassifiedTextLiteral(node);
        }
    }

    public override void VisitToken(SyntaxToken token)
    {
        if (_isWritingHtml)
        {
            WriteHtmlToken(token);
        }
        else
        {
            WriteNonHtmlToken(token);
        }
    }

    private readonly ref struct WriterScope
    {
        private readonly RazorHtmlWriter _writer;
        private readonly bool _oldIsWritingHtml;

        public WriterScope(RazorHtmlWriter writer, bool isWritingHtml)
        {
            _writer = writer;
            _oldIsWritingHtml = writer._isWritingHtml;
            writer._isWritingHtml = isWritingHtml;
        }

        public void Dispose()
        {
            _writer._isWritingHtml = _oldIsWritingHtml;
        }
    }

    private WriterScope NonHtmlScope()
        => new(this, isWritingHtml: false);

    private WriterScope HtmlScope()
        => new(this, isWritingHtml: true);

    private void WriteHtmlToken(SyntaxToken token)
    {
        var content = token.Content;
        if (content.Length == 0)
        {
            // If the token is empty, we don't need to do anything further.
            return;
        }

        WriteCSharpContentPlaceholder();

        _codeWriter.Write(content);
    }

    private void WriteNonHtmlToken(SyntaxToken token)
    {
        var content = token.Content.AsMemory();
        if (content.Length == 0)
        {
            // If the token is empty, we don't need to do anything further.
            return;
        }

        // To avoid allocating new strings, we want to write whitespace sliced from the original
        // token content. To achieve this, we track transitions between whitespace and non-whitespace
        // characters. When we're tracking whitespace, whitespaceIndex will be set to the index of the
        // last transition to whitespace. When we encounter a non-whitespace character, we write the
        // C# content placeholder (if any) followed by the whitespace. Then, we reset the whitespaceIndex to -1.

        var whitespaceIndex = -1;

        for (var i = 0; i < content.Length; i++)
        {
            var charIsWhitespace = char.IsWhiteSpace(content.Span[i]);

            if (charIsWhitespace)
            {
                // If we're transitioning from non-whitespace to whitespace, set the index.
                if (whitespaceIndex < 0)
                {
                    whitespaceIndex = i;
                }

                continue;
            }

            // At this point, we have a non-whitespace character. If we were tracking whitespace,
            // we need to write the C# content placeholder (if any) and the whitespace.
            if (whitespaceIndex >= 0)
            {
                WriteCSharpContentPlaceholder();
                _codeWriter.Write(content[whitespaceIndex..i]);

                // We're transitioning from whitespace to non-whitespace, so reset the index.
                whitespaceIndex = -1;
            }

            // Finally, be sure to increment the C# content placeholder size so that we can write it later.
            _placeholderSize++;
        }

        // If we finished processing the content but were still tracking whitespace,
        // we need to write the C# content placeholder and the whitespace content.
        if (whitespaceIndex >= 0)
        {
            WriteCSharpContentPlaceholder();
            _codeWriter.Write(content[whitespaceIndex..]);
        }
    }

    private void WriteCSharpContentPlaceholder()
    {
        var tildesToWrite = _placeholderSize;

        if (tildesToWrite == 0)
        {
            // Nothing to write, so just return
            return;
        }

        // Reset the placeholder size.
        _placeholderSize = 0;

        var writeComment = false;

        // When writing 4 or more tildes, we write them as a comment to relieve pressure on the JS/TS compiler.

        if (tildesToWrite >= 4)
        {
            // SPECIAL CASE: If the next character is a greater than symbol ('>'), we don't write a comment because
            // the forward slash in the comment would be interpreted as the end of a tag, resulting in incorrect HTML.
            // For example, `<div class=@className>` should not be written as `<div class=/*~~~~~*/>`.

            var nextIndex = _codeWriter.Location.AbsoluteIndex + tildesToWrite;
            var text = _source.Text;

            Debug.Assert(nextIndex <= text.Length, "The next index should not exceed the length of the source text.");

            if (nextIndex >= text.Length || text[nextIndex] != '>')
            {
                // We can write a comment
                writeComment = true;
                tildesToWrite -= 4; // We need to reserve 4 characters for the comment start and end
            }
        }

        if (writeComment)
        {
            _codeWriter.Write(CommentStart);
        }

        while (tildesToWrite > 0)
        {
            var tildes = tildesToWrite < Tildes.Length
                ? Tildes[..tildesToWrite]
                : Tildes;

            _codeWriter.Write(tildes);
            tildesToWrite -= tildes.Length;
        }

        if (writeComment)
        {
            _codeWriter.Write(CommentEnd);
        }
    }
}
