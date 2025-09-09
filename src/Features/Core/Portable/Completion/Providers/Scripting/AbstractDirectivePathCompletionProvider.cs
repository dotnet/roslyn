// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractDirectivePathCompletionProvider : CompletionProvider
{
    protected static bool IsDirectorySeparator(char ch)
         => ch == '/' || (ch == '\\' && !PathUtilities.IsUnixLikePlatform);

    protected abstract bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken);

    /// <summary>
    /// <code>r</code> for metadata reference directive, <code>load</code> for source file directive.
    /// </summary>
    protected abstract string DirectiveName { get; }

    public sealed override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            if (!TryGetStringLiteralToken(tree, position, out var stringLiteral, cancellationToken))
            {
                return;
            }

            var literalValue = stringLiteral.ToString();

            context.CompletionListSpan = GetTextChangeSpan(
                quotedPath: literalValue,
                quotedPathStart: stringLiteral.SpanStart,
                position: position);

            var pathThroughLastSlash = GetPathThroughLastSlash(
                quotedPath: literalValue,
                quotedPathStart: stringLiteral.SpanStart,
                position: position);

            await ProvideCompletionsAsync(context, pathThroughLastSlash).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }

    public sealed override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
    {
        var lineStart = text.Lines.GetLineFromPosition(caretPosition).Start;

        // check if the line starts with {whitespace}#{whitespace}{DirectiveName}{whitespace}"

        var poundIndex = text.IndexOfNonWhiteSpace(lineStart, caretPosition - lineStart);
        if (poundIndex == -1 || text[poundIndex] != '#')
        {
            return false;
        }

        var directiveNameStartIndex = text.IndexOfNonWhiteSpace(poundIndex + 1, caretPosition - poundIndex - 1);
        if (directiveNameStartIndex == -1 || !text.ContentEquals(directiveNameStartIndex, DirectiveName))
        {
            return false;
        }

        var directiveNameEndIndex = directiveNameStartIndex + DirectiveName.Length;
        var quoteIndex = text.IndexOfNonWhiteSpace(directiveNameEndIndex, caretPosition - directiveNameEndIndex);
        if (quoteIndex == -1 || text[quoteIndex] != '"')
        {
            return false;
        }

        return true;
    }

    private static string GetPathThroughLastSlash(string quotedPath, int quotedPathStart, int position)
    {
        Contract.ThrowIfTrue(quotedPath[0] != '"');

        const int QuoteLength = 1;

        var positionInQuotedPath = position - quotedPathStart;
        var path = quotedPath[QuoteLength..positionInQuotedPath].Trim();
        var afterLastSlashIndex = AfterLastSlashIndex(path, path.Length);

        // We want the portion up to, and including the last slash if there is one.  That way if
        // the user pops up completion in the middle of a path (i.e. "C:\Win") then we'll
        // consider the path to be "C:\" and we will show appropriate completions.
        return afterLastSlashIndex >= 0 ? path[..afterLastSlashIndex] : path;
    }

    private static TextSpan GetTextChangeSpan(string quotedPath, int quotedPathStart, int position)
    {
        // We want the text change to be from after the last slash to the end of the quoted
        // path. If there is no last slash, then we want it from right after the start quote
        // character.
        var positionInQuotedPath = position - quotedPathStart;

        // Where we want to start tracking is right after the slash (if we have one), or else
        // right after the string starts.
        var afterLastSlashIndex = AfterLastSlashIndex(quotedPath, positionInQuotedPath);
        var afterFirstQuote = 1;

        var startIndex = Math.Max(afterLastSlashIndex, afterFirstQuote);
        var endIndex = quotedPath.Length;

        // If the string ends with a quote, the we do not want to consume that.
        if (EndsWithQuote(quotedPath))
        {
            endIndex--;
        }

        return TextSpan.FromBounds(startIndex + quotedPathStart, endIndex + quotedPathStart);
    }

    private static bool EndsWithQuote(string quotedPath)
        => quotedPath is [.., _, '"'];

    /// <summary>
    /// Returns the index right after the last slash that precedes 'position'.  If there is no
    /// slash in the string, -1 is returned.
    /// </summary>
    private static int AfterLastSlashIndex(string text, int position)
    {
        // Position might be out of bounds of the string (if the string is unterminated.  Make
        // sure it's within bounds.
        position = Math.Min(position, text.Length - 1);

        int index;
        if ((index = text.LastIndexOf('/', position)) >= 0 ||
            !PathUtilities.IsUnixLikePlatform && (index = text.LastIndexOf('\\', position)) >= 0)
        {
            return index + 1;
        }

        return -1;
    }

    protected abstract Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash);

    protected static FileSystemCompletionHelper GetFileSystemCompletionHelper(
        Document document,
        Glyph itemGlyph,
        ImmutableArray<string> extensions,
        CompletionItemRules completionRules)
    {
        ImmutableArray<string> referenceSearchPaths;
        string? baseDirectory;
        if (document.Project.CompilationOptions?.MetadataReferenceResolver is RuntimeMetadataReferenceResolver resolver)
        {
            referenceSearchPaths = resolver.PathResolver.SearchPaths;
            baseDirectory = resolver.PathResolver.BaseDirectory;
        }
        else
        {
            referenceSearchPaths = [];
            baseDirectory = null;
        }

        return new FileSystemCompletionHelper(
            Glyph.OpenFolder,
            itemGlyph,
            referenceSearchPaths,
            GetBaseDirectory(document, baseDirectory),
            extensions,
            completionRules);
    }

    private static string? GetBaseDirectory(Document document, string? baseDirectory)
    {
        var result = PathUtilities.GetDirectoryName(document.FilePath);
        if (!PathUtilities.IsAbsolute(result))
        {
            result = baseDirectory;
            Debug.Assert(result == null || PathUtilities.IsAbsolute(result));
        }

        return result;
    }
}
