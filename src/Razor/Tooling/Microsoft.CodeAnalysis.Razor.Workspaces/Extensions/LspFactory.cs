// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.LanguageServer.Protocol;

internal static class LspFactory
{
    private static readonly Position s_defaultPosition = new(0, 0);

    private static readonly LspRange s_defaultRange = new()
    {
        Start = s_defaultPosition,
        End = s_defaultPosition
    };

    private static readonly Position s_undefinedPosition = new(-1, -1);

    private static readonly LspRange s_undefinedRange = new()
    {
        Start = s_undefinedPosition,
        End = s_undefinedPosition
    };

    /// <summary>
    ///  Returns a <see cref="Position"/> for line 0 and character 0.
    /// </summary>
    public static Position DefaultPosition
    {
        get
        {
            var defaultPosition = s_defaultPosition;

            // Since Position is mutable, it's possible that something might modify it. If that happens, we should know!
            Debug.Assert(
                defaultPosition.Line == 0 &&
                defaultPosition.Character == 0,
                $"{nameof(LspFactory)}.{nameof(DefaultPosition)} has been corrupted. Current value: {defaultPosition.ToDisplayString()}");

            return defaultPosition;
        }
    }

    /// <summary>
    ///  Returns a <see cref="Position"/> for starting line 0 and character 0,
    ///  and ending line 0 and character 0.
    /// </summary>
    public static LspRange DefaultRange
    {
        get
        {
            var defaultRange = s_defaultRange;

            // Since Range is mutable, it's possible that something might modify it. If that happens, we should know!
            Debug.Assert(
                defaultRange.Start.Line == 0 &&
                defaultRange.Start.Character == 0 &&
                defaultRange.End.Line == 0 &&
                defaultRange.End.Character == 0,
                $"{nameof(LspFactory)}.{nameof(DefaultRange)} has been corrupted. Current value: {defaultRange.ToDisplayString()}");

            return defaultRange;
        }
    }

    public static Position UndefinedPosition
    {
        get
        {
            var undefinedPosition = s_undefinedPosition;

            // Since Position is mutable, it's possible that something might modify it. If that happens, we should know!
            Debug.Assert(
                undefinedPosition.Line == -1 &&
                undefinedPosition.Character == -1,
                $"{nameof(LspFactory)}.{nameof(UndefinedPosition)} has been corrupted. Current value: {undefinedPosition.ToDisplayString()}");

            return undefinedPosition;
        }
    }

    public static LspRange UndefinedRange
    {
        get
        {
            var undefinedRange = s_undefinedRange;

            // Since Range is mutable, it's possible that something might modify it. If that happens, we should know!
            Debug.Assert(
                undefinedRange.Start.Line == -1 &&
                undefinedRange.Start.Character == -1 &&
                undefinedRange.End.Line == -1 &&
                undefinedRange.End.Character == -1,
                $"{nameof(LspFactory)}.{nameof(UndefinedRange)} has been corrupted. Current value: {undefinedRange.ToDisplayString()}");

            return undefinedRange;
        }
    }

    public static Position CreatePosition(int line, int character)
        => (line, character) switch
        {
            (0, 0) => DefaultPosition,
            (-1, -1) => UndefinedPosition,
            _ => new(line, character)
        };

    public static Position CreatePosition(LinePosition linePosition)
        => CreatePosition(linePosition.Line, linePosition.Character);

    public static Position CreatePosition((int line, int character) position)
        => CreatePosition(position.line, position.character);

    public static LspRange CreateRange(int startLine, int startCharacter, int endLine, int endCharacter)
        => startLine == endLine && startCharacter == endCharacter
            ? CreateZeroWidthRange(startLine, startCharacter)
            : CreateRange(CreatePosition(startLine, startCharacter), CreatePosition(endLine, endCharacter));

    public static LspRange CreateRange(Position start, Position end)
        => new() { Start = start, End = end };

    public static LspRange CreateRange(LinePosition start, LinePosition end)
        => CreateRange(start.Line, start.Character, end.Line, end.Character);

    public static LspRange CreateRange((int line, int character) start, (int line, int character) end)
        => CreateRange(start.line, start.character, end.line, end.character);

    public static LspRange CreateRange(LinePositionSpan span)
        => CreateRange(span.Start, span.End);

    public static LspRange CreateZeroWidthRange(int line, int character)
        => (line, character) switch
        {
            (0, 0) => DefaultRange,
            (-1, -1) => UndefinedRange,
            _ => CreateZeroWidthRange(CreatePosition(line, character))
        };

    public static LspRange CreateZeroWidthRange(Position position)
        => CreateRange(position, position);

    public static LspRange CreateZeroWidthRange(LinePosition position)
        => CreateRange(position, position);

    public static LspRange CreateZeroWidthRange((int line, int character) position)
        => CreateRange(position, position);

    public static LspRange CreateSingleLineRange(int line, int character, int length)
        => CreateRange(line, character, line, character + length);

    public static LspRange CreateSingleLineRange(Position start, int length)
        => CreateRange(start, CreatePosition(start.Line, start.Character + length));

    public static LspRange CreateSingleLineRange(LinePosition start, int length)
        => CreateSingleLineRange(start.Line, start.Character, length);

    public static LspRange CreateSingleLineRange((int line, int character) start, int length)
        => CreateRange(CreatePosition(start), CreatePosition(start.line, start.character + length));

    public static LspLocation CreateLocation(string filePath, LinePositionSpan span)
        => CreateLocation(CreateFilePathUri(filePath), CreateRange(span));

    public static LspLocation CreateLocation(Uri uri, LinePositionSpan span)
        => CreateLocation(uri, CreateRange(span));

    public static LspLocation CreateLocation(string filePath, LspRange range)
        => CreateLocation(CreateFilePathUri(filePath), range);

    public static LspLocation CreateLocation(Uri uri, LspRange range)
        => new() { DocumentUri = new(uri), Range = range };

    public static DocumentLink CreateDocumentLink(Uri target, LspRange range)
        => new() { DocumentTarget = new(target), Range = range };

    public static DocumentLink CreateDocumentLink(Uri target, LinePositionSpan span)
        => new() { DocumentTarget = new(target), Range = CreateRange(span) };

    public static TextEdit CreateTextEdit(Range range, string newText)
        => new() { Range = range, NewText = newText };

    public static TextEdit CreateTextEdit(LinePositionSpan span, string newText)
        => CreateTextEdit(CreateRange(span), newText);

    public static TextEdit CreateTextEdit(int startLine, int startCharacter, int endLine, int endCharacter, string newText)
        => CreateTextEdit(CreateRange(startLine, startCharacter, endLine, endCharacter), newText);

    public static TextEdit CreateTextEdit(Position start, Position end, string newText)
        => CreateTextEdit(CreateRange(start, end), newText);

    public static TextEdit CreateTextEdit(LinePosition start, LinePosition end, string newText)
        => CreateTextEdit(CreateRange(start, end), newText);

    public static TextEdit CreateTextEdit(int line, int character, string newText)
        => CreateTextEdit(CreateZeroWidthRange(line, character), newText);

    public static TextEdit CreateTextEdit(Position position, string newText)
        => CreateTextEdit(CreateZeroWidthRange(position), newText);

    public static TextEdit CreateTextEdit(LinePosition position, string newText)
        => CreateTextEdit(CreateZeroWidthRange(position.Line, position.Character), newText);

    public static TextEdit CreateTextEdit((int line, int character) position, string newText)
        => CreateTextEdit(CreateZeroWidthRange(position), newText);

    public static Uri CreateFilePathUri(string filePath, LanguageServerFeatureOptions options)
    {
        // VS Code in Windows expects path to start with '/'
        var updateFilePath = options.ReturnCodeActionAndRenamePathsWithPrefixedSlash && !filePath.StartsWith('/')
            ? $"/{filePath}"
            : filePath;

        return CreateFilePathUri(updateFilePath);
    }

    public static Uri CreateFilePathUri(string filePath)
    {
        var builder = new UriBuilder
        {
            Path = filePath,
            Scheme = Uri.UriSchemeFile,
            Host = string.Empty,
        };

        return builder.Uri;
    }

    public static FoldingRange CreateFoldingRange(FoldingRangeKind kind, LinePositionSpan linePositionSpan)
        => new()
        {
            Kind = kind,
            StartLine = linePositionSpan.Start.Line,
            StartCharacter = linePositionSpan.Start.Character,
            EndLine = linePositionSpan.End.Line,
            EndCharacter = linePositionSpan.End.Character
        };
}
