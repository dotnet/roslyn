// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.ProjectTools;
using static Microsoft.DotNet.FileBasedPrograms.FileBasedProgramDirectiveValueHelpers;

namespace Microsoft.DotNet.FileBasedPrograms;

internal static class FileLevelDirectiveHelpers
{
    public static SyntaxTokenParser CreateTokenizer(SourceText text)
    {
        return SyntaxFactory.CreateTokenParser(text,
            CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]));
    }

    /// <param name="reportAllErrors">
    /// If <see langword="true"/>, the whole <paramref name="sourceFile"/> is parsed to find diagnostics about every app directive.
    /// Otherwise, only directives up to the first C# token is checked.
    /// The former is useful for <c>dotnet project convert</c> where we want to report all errors because it would be difficult to fix them up after the conversion.
    /// The latter is useful for <c>dotnet run file.cs</c> where if there are app directives after the first token,
    /// compiler reports <see cref="ErrorCode.ERR_PPIgnoredFollowsToken"/> anyway, so we speed up success scenarios by not parsing the whole file up front in the SDK CLI.
    /// </param>
    public static ImmutableArray<CSharpDirective> FindDirectives(SourceFile sourceFile, bool reportAllErrors, ErrorReporter errorReporter, bool checkDuplicates = true)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpDirective>();
        using var tokenizer = CreateTokenizer(sourceFile.Text);

        var result = tokenizer.ParseLeadingTrivia();
        var triviaList = result.Token.LeadingTrivia;

        tokenizer.ResetTo(result);

        FindLeadingDirectives(sourceFile, triviaList, errorReporter, builder, checkDuplicates, tokenizer);

        // In conversion mode, we want to report errors for any invalid directives in the rest of the file
        // so users don't end up with invalid directives in the converted project.
        if (reportAllErrors)
        {
            tokenizer.ResetTo(result);

            do
            {
                result = tokenizer.ParseNextToken();

                foreach (var trivia in result.Token.LeadingTrivia)
                {
                    ReportErrorFor(trivia);
                }

                foreach (var trivia in result.Token.TrailingTrivia)
                {
                    ReportErrorFor(trivia);
                }
            }
            while (!result.Token.IsKind(SyntaxKind.EndOfFileToken));
        }

        void ReportErrorFor(SyntaxTrivia trivia)
        {
            if (trivia.ContainsDiagnostics && trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                errorReporter(sourceFile.Text, sourceFile.Path, trivia.Span, FileBasedProgramsResources.CannotConvertDirective);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>Finds file-level directives in the leading trivia list of a compilation unit and reports diagnostics on them.</summary>
    /// <param name="builder">The builder to store the parsed directives in, or null if the parsed directives are not needed.</param>
    public static void FindLeadingDirectives(
        SourceFile sourceFile,
        SyntaxTriviaList triviaList,
        ErrorReporter errorReporter,
        ImmutableArray<CSharpDirective>.Builder? builder,
        bool checkDuplicates = true,
        SyntaxTokenParser? tokenizer = null)
    {
        var deduplicator = new DirectiveDeduplicator();
        TextSpan previousWhiteSpaceSpan = default;
        using var valueLexer = new DirectiveValueLexer(sourceFile.Text, tokenizer);

        for (var index = 0; index < triviaList.Count; index++)
        {
            var trivia = triviaList[index];
            // Stop when the trivia contains an error (e.g., because it's after #if).
            if (trivia.ContainsDiagnostics)
            {
                break;
            }

            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                Debug.Assert(previousWhiteSpaceSpan.IsEmpty);
                previousWhiteSpaceSpan = trivia.FullSpan;
                continue;
            }

            if (trivia.IsKind(SyntaxKind.ShebangDirectiveTrivia))
            {
                TextSpan span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                var whiteSpace = GetWhiteSpaceInfo(triviaList, index, span);
                var info = new CSharpDirective.ParseInfo
                {
                    SourceFile = sourceFile,
                    Span = span,
                    LeadingWhiteSpace = whiteSpace.Leading,
                    TrailingWhiteSpace = whiteSpace.Trailing,
                };
                builder?.Add(new CSharpDirective.Shebang(info));
            }
            else if (trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                TextSpan span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                ReadOnlySpan<char> message;
                int messageStart;
                if (trivia.GetStructure() is IgnoredDirectiveTriviaSyntax { Content: { RawKind: (int)SyntaxKind.StringLiteralToken } content })
                {
                    var contentText = content.Text.AsSpan();
                    var trimmedStart = contentText.TrimStart();
                    message = trimmedStart.TrimEnd();
                    messageStart = content.SpanStart + (contentText.Length - trimmedStart.Length);
                }
                else
                {
                    message = default;
                    messageStart = 0;
                }

                var parts = Patterns.Whitespace.Split(message.ToString(), 2);
                var name = parts.Length > 0 ? parts[0] : "";
                var value = parts.Length > 1 ? parts[1] : "";
                Debug.Assert(!(parts.Length > 2));

                var whiteSpace = GetWhiteSpaceInfo(triviaList, index, span);
                var context = new CSharpDirective.ParseContext
                {
                    Info = new()
                    {
                        SourceFile = sourceFile,
                        Span = span,
                        LeadingWhiteSpace = whiteSpace.Leading,
                        TrailingWhiteSpace = whiteSpace.Trailing,
                    },
                    ErrorReporter = errorReporter,
                    DirectiveKind = name,
                    DirectiveText = value,
                    DirectiveTextStart = messageStart + (message.Length - value.Length),
                    ValueLexer = valueLexer,
                };

                if (CSharpDirective.Parse(context) is { } directive)
                {
                    if (checkDuplicates)
                    {
                        deduplicator.CheckDirective(directive, errorReporter, shouldKeep: out _);
                    }

                    builder?.Add(directive);
                }
            }

            previousWhiteSpaceSpan = default;
        }

        return;

        static TextSpan GetFullSpan(TextSpan previousWhiteSpaceSpan, SyntaxTrivia trivia)
        {
            // Include the preceding whitespace in the span, i.e., span will be the whole line.
            return previousWhiteSpaceSpan.IsEmpty ? trivia.FullSpan : TextSpan.FromBounds(previousWhiteSpaceSpan.Start, trivia.FullSpan.End);
        }

        static (WhiteSpaceInfo Leading, WhiteSpaceInfo Trailing) GetWhiteSpaceInfo(in SyntaxTriviaList triviaList, int index, TextSpan excludeSpan)
        {
            (WhiteSpaceInfo Leading, WhiteSpaceInfo Trailing) result = default;

            for (int i = index - 1; i >= 0; i--)
            {
                if (!Fill(ref result.Leading, triviaList, i, excludeSpan)) break;
            }

            for (int i = index + 1; i < triviaList.Count; i++)
            {
                if (!Fill(ref result.Trailing, triviaList, i, excludeSpan)) break;
            }

            return result;

            static bool Fill(ref WhiteSpaceInfo info, in SyntaxTriviaList triviaList, int index, TextSpan excludeSpan)
            {
                var trivia = triviaList[index];

                var length = trivia.FullSpan.Length - (trivia.FullSpan.Intersection(excludeSpan)?.Length ?? 0);

                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    if (length != 0)
                    {
                        info.BlankLineLength += info.RestLength + length;
                        info.RestLength = 0;
                    }

                    return true;
                }

                if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    info.RestLength += length;
                    return true;
                }

                return false;
            }
        }
    }
}

internal readonly record struct SourceFile(string Path, SourceText Text)
{
    public static SourceFile Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        // Let SourceText.From auto-detect the encoding (including BOM detection)
        return new SourceFile(filePath, SourceText.From(stream, encoding: null));
    }

    public void Save()
    {
        using var stream = File.Open(Path, FileMode.Create, FileAccess.Write);
        // Use the encoding from SourceText, which preserves the original BOM state
        var encoding = Text.Encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using var writer = new StreamWriter(stream, encoding);
        Text.Write(writer);
    }

    public string GetLocationString(TextSpan span)
    {
        return $"{Path}({Text.Lines.GetLinePositionSpan(span).Start.Line + 1})";
    }
}

internal static partial class Patterns
{
    public static Regex Whitespace { get; } = new Regex("""\s+""", RegexOptions.Compiled);

    public static Regex EscapedCompilerOption { get; } = new Regex("""^/\w+:".*"$""", RegexOptions.Compiled | RegexOptions.Singleline);
}

internal sealed class DirectiveValueLexer(SourceText text, SyntaxTokenParser? parser) : IDisposable
{
    private readonly bool _ownsParser = parser is null;
    private SyntaxTokenParser? _parser = parser;
    private SyntaxTokenParser.Result? _previous;

    /// <summary>
    /// Lexes a single token starting at <paramref name="position"/>,
    /// which must be at or after the position of the previously lexed token
    /// (directive values are always requested in source order).
    /// </summary>
    public SyntaxToken LexStringLiteral(int position)
    {
        _parser ??= FileLevelDirectiveHelpers.CreateTokenizer(text);

        // ParseNextToken also consumes the token's trailing trivia,
        // which for a '//' comment runs to the end of the line,
        // so the lexer can end up past the position wanted next.
        if (_previous is { } previous)
        {
            Debug.Assert(position >= previous.Token.FullSpan.Start);
            _parser.ResetTo(previous);
        }

        _parser.SkipForwardTo(position);
        var result = _parser.ParseNextToken();
        _previous = result;
        return result.Token;
    }

    public void Dispose()
    {
        if (_ownsParser)
        {
            _parser?.Dispose();
        }
    }
}

internal struct WhiteSpaceInfo
{
    /// <summary>
    /// Size of whitespace that consists of only blank lines (i.e., lines that contain only whitespace).
    /// </summary>
    public int BlankLineLength;

    /// <summary>
    /// Size of the remaining whitespace on a not-entirely-blank line.
    /// </summary>
    public int RestLength;
}

/// <summary>
/// Represents a C# directive starting with <c>#:</c> (a.k.a., "file-level directive").
/// Those are ignored by the language but recognized by us.
/// </summary>
internal abstract class CSharpDirective(in CSharpDirective.ParseInfo info)
{
    internal static readonly StringComparer MetadataNameComparer = StringComparer.OrdinalIgnoreCase;
    internal static readonly StringComparer MetadataValueComparer = StringComparer.Ordinal;

    public ParseInfo Info { get; } = info;

    public readonly struct ParseInfo
    {
        public required SourceFile SourceFile { get; init; }

        /// <summary>
        /// Span of the full line including the trailing line break.
        /// </summary>
        public required TextSpan Span { get; init; }

        /// <summary>
        /// Additional leading whitespace not included in <see cref="Span"/>.
        /// </summary>
        public required WhiteSpaceInfo LeadingWhiteSpace { get; init; }

        /// <summary>
        /// Additional trailing whitespace not included in <see cref="Span"/>.
        /// </summary>
        public required WhiteSpaceInfo TrailingWhiteSpace { get; init; }
    }

    public readonly struct ParseContext
    {
        public required ParseInfo Info { get; init; }
        public required ErrorReporter ErrorReporter { get; init; }
        public required string DirectiveKind { get; init; }
        public required string DirectiveText { get; init; }

        /// <summary>
        /// Position of <see cref="DirectiveText"/> within <see cref="ParseInfo.SourceFile"/>'s text.
        /// </summary>
        public required int DirectiveTextStart { get; init; }

        /// <summary>
        /// Lexer shared by all directives of one parse operation, used to lex quoted values.
        /// </summary>
        public required DirectiveValueLexer ValueLexer { get; init; }

        public void ReportError(string message)
            => ErrorReporter(Info.SourceFile.Text, Info.SourceFile.Path, Info.Span, message);

        public void ReportError(TextSpan span, string message)
            => ErrorReporter(Info.SourceFile.Text, Info.SourceFile.Path, span, message);
    }

    public static Named? Parse(in ParseContext context)
    {
        switch (context.DirectiveKind)
        {
            case "sdk": return Sdk.Parse(context);
            case "property": return Property.Parse(context);
            case "package": return Package.Parse(context);
            case "project": return Project.Parse(context);
            case "ref": return Ref.Parse(context);
            case "include" or "exclude": return IncludeOrExclude.Parse(context);
            default:
                context.ReportError(string.Format(FileBasedProgramsResources.UnrecognizedDirective, context.DirectiveKind));
                return null;
        }
    }

    /// <summary>
    /// One whitespace-separated token of a directive, e.g., <c>Package@1.0.0</c> or <c>Name=Value</c>.
    /// </summary>
    /// <param name="Text">
    /// The token with any quotes removed and their escape sequences decoded.
    /// </param>
    /// <param name="SeparatorIndex">
    /// Index within <paramref name="Text"/> of the separator that splits the token into its name and value,
    /// or <c>-1</c> if the token has no separator.
    /// </param>
    private readonly record struct DirectiveToken(string Text, int SeparatorIndex)
    {
        /// <summary>
        /// Creates a token whose separator is located by searching <paramref name="text"/>,
        /// used for the legacy (unquoted) form where the tokenizer did not track the separator position.
        /// </summary>
        public static DirectiveToken Create(string text, char? separator)
            => new(text, separator is { } s ? text.IndexOf(s) : -1);
    }

    /// <summary>
    /// Splits <see cref="ParseContext.DirectiveText"/> into whitespace-separated tokens.
    /// <para>
    /// A token is a value (e.g. <c>../lib</c>), or a name and a value joined by the token's separator (e.g. <c>Package@1.0.0</c>, <c>Name=Value</c>).
    /// The separator is <paramref name="nameSeparator"/> for the first token, which differs per directive kind
    /// (<c>@</c> for <c>#:sdk</c>/<c>#:package</c>, <c>=</c> for <c>#:property</c>, and none for the kinds whose value is a path),
    /// and <c>=</c> for the trailing item-metadata tokens.
    /// Whitespace may surround the separator, so <c>A=B</c>, <c>A =B</c>, <c>A= B</c>, and <c>A = B</c> are equivalent.
    /// </para>
    /// <para>
    /// Each side of the separator is written either bare or wrapped entirely in double quotes (<c>"</c>), which lets it contain whitespace.
    /// A quoted part is lexed as a regular C# string literal,
    /// so escape sequences like <c>\"</c>, <c>\\</c> and <c>\t</c> are decoded; verbatim (<c>@"..."</c>) and raw (<c>"""..."""</c>) literals are not supported.
    /// A quote may therefore open only at the start of a part, and only the separator may follow a closing quote.
    /// So <c>A=B</c>, <c>A="B"</c>, and <c>"A"="B"</c> are allowed, but <c>A=B"C"</c> and <c>A="B"C</c> are errors.
    /// Returns <see langword="null"/> and reports an error if a quote is misplaced or left unterminated.
    /// </para>
    /// </summary>
    private static ImmutableArray<DirectiveToken>? Tokenize(in ParseContext context, char? nameSeparator, bool allowMetadata)
    {
        var text = context.DirectiveText;
        var tokens = ImmutableArray.CreateBuilder<DirectiveToken>();
        var current = new StringBuilder();
        var tokenStarted = false;
        var quoteClosed = false;
        var separatorIndex = -1;
        var afterSeparator = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            var separator = tokens.Count == 0 ? nameSeparator : '=';

            if (c == '"')
            {
                // A quoted part must be the whole token or one side of the token's separator.
                var atPartStart = current.Length == 0 || afterSeparator;
                if (quoteClosed || !atPartStart)
                {
                    context.ReportError(FileBasedProgramsResources.InvalidQuoteInDirective);
                    return null;
                }

                // Lex a regular C# string literal so the value can contain whitespace and use escape sequences.
                // Verbatim (@"...") literals can't start here (the '@' would precede the quote and fail the check above),
                // and raw ("""...""") literals lex to a different token kind and are rejected below.
                var token = context.ValueLexer.LexStringLiteral(context.DirectiveTextStart + i);
                var errors = token.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
                if (errors.Count > 0)
                {
                    // CS1010 ("Newline in constant") means the literal was left unterminated;
                    // give it our clearer directive-specific message.
                    // Forward Roslyn's message for any other lexer error (e.g. CS1009 for an invalid escape sequence).
                    if (errors.Any(static d => d.Id == "CS1010"))
                    {
                        context.ReportError(FileBasedProgramsResources.UnterminatedQuoteInDirective);
                    }
                    else
                    {
                        context.ReportError(string.Format(FileBasedProgramsResources.InvalidStringLiteralInDirective, errors[0].GetMessage()));
                    }

                    return null;
                }

                if (!token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    // Any token carrying a lexer error was already reported above,
                    // so the only thing that reaches here is a *well-formed* literal that starts with '"' yet isn't a simple string literal.
                    // Today that can only be a raw string literal ('"""..."""').
                    context.ReportError(string.Format(FileBasedProgramsResources.ExpectedSimpleStringLiteralInDirective, token.Text));
                    return null;
                }

                // The decoded value is appended to the current token (which may already hold a 'Name=' prefix);
                // a quote starts a token even if it is empty (e.g., '""' is an empty token).
                current.Append(token.ValueText);
                tokenStarted = true;
                quoteClosed = true;
                afterSeparator = false;
                i += token.Text.Length - 1;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                // Whitespace around the token's separator does not end the token, so
                // '#:property Name = "value"' is the same as '#:property Name="value"'.
                // After the separator it still ends the token when the next segment is a 'Name=Value' metadata pair,
                // so an empty value keeps its own token and does not swallow the metadata that follows it (e.g. '#:package Package@ ExcludeAssets=runtime').
                var beforeValue = afterSeparator && !(allowMetadata && StartsMetadataToken(text, i + 1));
                if (tokenStarted && (beforeValue || IsNextNonWhiteSpace(text, i, separator, separatorIndex)))
                {
                    continue;
                }

                if (tokenStarted)
                {
                    tokens.Add(new DirectiveToken(current.ToString(), separatorIndex));
                    current.Clear();
                    tokenStarted = false;
                    quoteClosed = false;
                    separatorIndex = -1;
                    afterSeparator = false;
                }

                continue;
            }

            // The separator ends the name part, so it may follow a closing quote (e.g., '"Humanizer"@2.0').
            // Only the first occurrence is structural; any later one belongs to the value (e.g., 'A=b=c').
            if (separator is { } s && c == s && separatorIndex < 0)
            {
                separatorIndex = current.Length;
                current.Append(c);
                tokenStarted = true;
                quoteClosed = false;
                afterSeparator = true;
                continue;
            }

            if (quoteClosed)
            {
                context.ReportError(FileBasedProgramsResources.InvalidQuoteInDirective);
                return null;
            }

            current.Append(c);
            tokenStarted = true;
            afterSeparator = false;
        }

        if (tokenStarted)
        {
            tokens.Add(new DirectiveToken(current.ToString(), separatorIndex));
        }

        return tokens.ToImmutable();

        // Returns whether the next non-whitespace character is a separator that the current token has not consumed yet,
        // i.e., whether the whitespace at 'index' merely precedes the separator.
        static bool IsNextNonWhiteSpace(string text, int index, char? separator, int separatorIndex)
        {
            if (separator is not { } s || separatorIndex >= 0)
            {
                return false;
            }

            for (var i = index + 1; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return text[i] == s;
                }
            }

            return false;
        }

        // Returns whether the next whitespace-delimited segment is a 'Name=Value' item-metadata pair.
        static bool StartsMetadataToken(string text, int index)
        {
            var start = index;
            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            var end = start;
            while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '=')
            {
                end++;
            }

            if (end == start)
            {
                return false;
            }

            var name = text.Substring(start, end - start);

            // The metadata separator may itself be preceded by whitespace, e.g., 'Note = "a b"'.
            while (end < text.Length && char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            return end < text.Length && text[end] == '=' && IsValidMSBuildName(name, out _);
        }
    }

    /// <summary>
    /// Tokenizes <see cref="ParseContext.DirectiveText"/> like <see cref="Tokenize"/> for the "new" form
    /// (which may use double quotes and/or trailing <c>Name=Value</c> metadata),
    /// but falls back to the pre-quoting "legacy" behavior to avoid a breaking change:
    /// before quoting and metadata were supported, a directive value could contain unquoted whitespace and was taken verbatim.
    /// <para>
    /// Rules (double quotes were previously disallowed, so their presence unambiguously means the new form):
    /// <list type="bullet">
    /// <item>If the text contains a double quote, it is parsed strictly via <see cref="Tokenize"/>.</item>
    /// <item>Otherwise, if there is at most one whitespace-separated token, it is returned as-is.</item>
    /// <item>Otherwise, the trailing tokens are treated as metadata
    /// only when <paramref name="allowMetadata"/> is set and every trailing token is a valid <c>Name=Value</c> pair;
    /// then the split tokens are returned.
    /// This is unlikely to be a breaking change as it requires a construct like
    /// <c>#:package X@1 A=B</c> (which would previously fail because space is disallowed in version)
    /// or <c>#:ref ./f.cs A=B</c> (which is unlikely to be a real path).</item>
    /// <item>Otherwise the whole (already trimmed) remainder is returned as a single legacy value
    /// with its internal whitespace preserved, and <paramref name="isLegacy"/> is set.
    /// The deprecated legacy form is flagged by an analyzer rather than erroring here.</item>
    /// </list>
    /// </para>
    /// </summary>
    private static ImmutableArray<DirectiveToken>? TokenizeWithLegacyFallback(
        in ParseContext context,
        char? nameSeparator,
        bool allowMetadata,
        out bool isLegacy)
    {
        isLegacy = false;
        var text = context.DirectiveText;

        // Quoting is the "new" form; parse strictly with full validation once a quote is present.
        if (text.Contains('"'))
        {
            return Tokenize(context, nameSeparator, allowMetadata);
        }

        if (text.Length == 0)
        {
            return ImmutableArray<DirectiveToken>.Empty;
        }

        var rawTokens = ImmutableArray.CreateRange(Patterns.Whitespace.Split(text));

        // A single token (no internal whitespace) is unambiguous.
        if (rawTokens.Length <= 1)
        {
            return ToLegacyTokens(rawTokens, nameSeparator);
        }

        // Multiple unquoted whitespace-separated tokens.
        // Interpret the trailing ones as item metadata only when metadata is supported and every trailing token is a valid 'Name=Value' pair.
        if (allowMetadata && AllValidMetadata(rawTokens, start: 1))
        {
            return ToLegacyTokens(rawTokens, nameSeparator);
        }

        // Legacy: the whole remainder is a single value (preserves pre-quoting behavior).
        isLegacy = true;
        return ImmutableArray.Create(DirectiveToken.Create(text, nameSeparator));

        static ImmutableArray<DirectiveToken> ToLegacyTokens(ImmutableArray<string> rawTokens, char? nameSeparator)
        {
            var builder = ImmutableArray.CreateBuilder<DirectiveToken>(rawTokens.Length);
            for (var i = 0; i < rawTokens.Length; i++)
            {
                builder.Add(DirectiveToken.Create(rawTokens[i], i == 0 ? nameSeparator : '='));
            }

            return builder.MoveToImmutable();
        }
    }

    /// <summary>
    /// Splits the first of <paramref name="tokens"/> into a required name and optional value
    /// on the first occurrence of <paramref name="separator"/> (e.g., <c>Name@Version</c>), validating the name.
    /// Used by <c>#:sdk</c>, <c>#:property</c>, and <c>#:package</c>.
    /// When <paramref name="trimAroundSeparator"/> is set (legacy form, where the token may contain unquoted whitespace),
    /// whitespace adjacent to the separator is trimmed to match the pre-quoting behavior.
    /// </summary>
    private static (string Name, string? Value)? ParseNameAndValue(in ParseContext context, ImmutableArray<DirectiveToken> tokens, char separator, bool trimAroundSeparator = false)
    {
        if (tokens.Length == 0)
        {
            context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, context.DirectiveKind));
            return null;
        }

        var (text, separatorIndex) = tokens[0];
        var name = separatorIndex < 0 ? text : text.Substring(0, separatorIndex);
        if (trimAroundSeparator)
        {
            name = name.TrimEnd();
        }

        if (name.Length == 0)
        {
            context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, context.DirectiveKind));
            return null;
        }

        // If the name contains characters that resemble separators, report an error to avoid any confusion.
        if (ContainsDisallowedNameCharacter(name))
        {
            context.ReportError(string.Format(FileBasedProgramsResources.InvalidDirectiveName, context.DirectiveKind, separator));
            return null;
        }

        var value = separatorIndex < 0 ? null : text.Substring(separatorIndex + 1);
        if (trimAroundSeparator && value is not null)
        {
            value = value.TrimStart();
        }

        return (name, value);
    }

    /// <summary>
    /// Parses the trailing <paramref name="tokens"/> (starting at <paramref name="start"/>) as <c>Name=Value</c> item metadata pairs.
    /// Returns <see langword="default"/> and reports an error if a token is not a valid metadata pair.
    /// </summary>
    private static ImmutableArray<(string Name, string Value)> ParseMetadata(
        in ParseContext context,
        ImmutableArray<DirectiveToken> tokens,
        int start,
        string? conflictingName = null)
    {
        if (start >= tokens.Length)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<(string Name, string Value)>(tokens.Length - start);
        var names = new HashSet<string>(MetadataNameComparer);

        for (var i = start; i < tokens.Length; i++)
        {
            var (text, separatorIndex) = tokens[i];
            if (separatorIndex <= 0)
            {
                context.ReportError(string.Format(FileBasedProgramsResources.InvalidDirectiveMetadata, text));
                return default;
            }

            var name = text.Substring(0, separatorIndex);
            var value = text.Substring(separatorIndex + 1);

            if (!IsValidMSBuildName(name, out var nameError))
            {
                context.ReportError(string.Format(FileBasedProgramsResources.DirectiveMetadataInvalidName, name, nameError));
                return default;
            }

            if (MetadataNameComparer.Equals(name, conflictingName))
            {
                context.ReportError(string.Format(FileBasedProgramsResources.ConflictingDirectiveMetadata, name));
                return default;
            }

            if (!names.Add(name))
            {
                context.ReportError(string.Format(FileBasedProgramsResources.DuplicateDirectiveMetadata, name));
                return default;
            }

            builder.Add((name, value));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Parses a directive that expects exactly one token (its value) and no metadata.
    /// Reports an error and returns <see langword="null"/> on empty or extra tokens.
    /// Unquoted whitespace is accepted as part of the value for backward compatibility
    /// (see <see cref="TokenizeWithLegacyFallback"/>).
    /// </summary>
    private static string? ParseSingleValue(in ParseContext context)
    {
        if (TokenizeWithLegacyFallback(context, nameSeparator: null, allowMetadata: false, out _) is not { } tokens)
        {
            return null;
        }

        if (tokens.Length == 0 || tokens[0].Text.Length == 0)
        {
            context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, context.DirectiveKind));
            return null;
        }

        if (tokens.Length > 1)
        {
            context.ReportError(string.Format(FileBasedProgramsResources.UnexpectedDirectiveText, context.DirectiveKind));
            return null;
        }

        return tokens[0].Text;
    }

    private static void AppendMetadata(StringBuilder builder, ImmutableArray<(string Name, string Value)> metadata)
    {
        if (metadata.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var (name, value) in metadata)
        {
            builder.Append(' ').Append(name).Append('=').Append(QuoteIfNeeded(value));
        }
    }

    public abstract override string ToString();

    public virtual string KindToString() => GetType().Name.ToLowerInvariant();

    /// <summary>
    /// <c>#!</c> directive.
    /// </summary>
    public sealed class Shebang(in ParseInfo info) : CSharpDirective(info)
    {
        public override string ToString() => "#!";
    }

    public abstract class Named(in ParseInfo info) : CSharpDirective(info)
    {
        public required string Name { get; init; }
    }

    /// <summary>
    /// <c>#:sdk</c> directive.
    /// </summary>
    public sealed class Sdk(in ParseInfo info) : Named(info)
    {
        public string? Version { get; init; }

        public static new Sdk? Parse(in ParseContext context)
        {
            if (TokenizeWithLegacyFallback(context, nameSeparator: '@', allowMetadata: false, out var isLegacy) is not { } tokens)
            {
                return null;
            }

            if (tokens.Length > 1)
            {
                context.ReportError(string.Format(FileBasedProgramsResources.UnexpectedDirectiveText, context.DirectiveKind));
                return null;
            }

            if (ParseNameAndValue(context, tokens, separator: '@', trimAroundSeparator: isLegacy) is not var (sdkName, sdkVersion))
            {
                return null;
            }

            return new Sdk(context.Info)
            {
                Name = sdkName,
                Version = sdkVersion,
            };
        }

        public override string ToString() => Version is null ? $"#:sdk {QuoteIfNeeded(Name)}" : $"#:sdk {QuoteIfNeeded(Name)}@{QuoteIfNeeded(Version)}";
    }

    /// <summary>
    /// <c>#:property</c> directive.
    /// </summary>
    public sealed class Property(in ParseInfo info) : Named(info)
    {
        public required string Value { get; init; }

        public static new Property? Parse(in ParseContext context)
        {
            if (TokenizeWithLegacyFallback(context, nameSeparator: '=', allowMetadata: false, out var isLegacy) is not { } tokens)
            {
                return null;
            }

            if (tokens.Length > 1)
            {
                context.ReportError(string.Format(FileBasedProgramsResources.UnexpectedDirectiveText, context.DirectiveKind));
                return null;
            }

            if (ParseNameAndValue(context, tokens, separator: '=', trimAroundSeparator: isLegacy) is not var (propertyName, propertyValue))
            {
                return null;
            }

            if (propertyValue is null)
            {
                context.ReportError(FileBasedProgramsResources.PropertyDirectiveMissingParts);
                return null;
            }

            if (!IsValidMSBuildName(propertyName, out var nameError))
            {
                context.ReportError(string.Format(FileBasedProgramsResources.PropertyDirectiveInvalidName, nameError));
                return null;
            }

            if (propertyName.Equals("RestoreUseStaticGraphEvaluation", StringComparison.OrdinalIgnoreCase) &&
                MSBuildUtilities.ConvertStringToBool(propertyValue))
            {
                context.ReportError(FileBasedProgramsResources.StaticGraphRestoreNotSupported);
            }

            return new Property(context.Info)
            {
                Name = propertyName,
                Value = propertyValue,
            };
        }

        public override string ToString() => $"#:property {Name}={QuoteIfNeeded(Value)}";
    }

    /// <summary>
    /// <c>#:package</c> directive.
    /// </summary>
    public sealed class Package(in ParseInfo info) : Named(info)
    {
        public string? Version { get; init; }

        /// <summary>
        /// Additional item metadata specified as trailing <c>Name=Value</c> pairs,
        /// e.g. <c>#:package Package@1.0.0 ExcludeAssets=runtime PrivateAssets=all</c>.
        /// </summary>
        public ImmutableArray<(string Name, string Value)> Metadata { get; init; }

        public static new Package? Parse(in ParseContext context)
        {
            if (TokenizeWithLegacyFallback(context, nameSeparator: '@', allowMetadata: true, out var isLegacy) is not { } tokens)
            {
                return null;
            }

            if (ParseNameAndValue(context, tokens, separator: '@', trimAroundSeparator: isLegacy) is not var (packageName, packageVersion))
            {
                return null;
            }

            if (ParseMetadata(context, tokens, start: 1, conflictingName: packageVersion is null ? null : "Version") is not { IsDefault: false } metadata)
            {
                return null;
            }

            return new Package(context.Info)
            {
                Name = packageName,
                Version = packageVersion,
                Metadata = metadata,
            };
        }

        public override string ToString()
        {
            var builder = new StringBuilder("#:package ");
            builder.Append(QuoteIfNeeded(Name));
            if (Version is not null)
            {
                builder.Append('@').Append(QuoteIfNeeded(Version));
            }

            AppendMetadata(builder, Metadata);
            return builder.ToString();
        }
    }

    /// <summary>
    /// <c>#:project</c> directive.
    /// </summary>
    public sealed class Project : Named
    {
        [SetsRequiredMembers]
        public Project(in ParseInfo info, string name) : base(info)
        {
            Name = name;
            OriginalName = name;
        }

        /// <summary>
        /// Preserved across <see cref="WithName"/> calls, i.e.,
        /// this is the original directive text as entered by the user.
        /// </summary>
        public string OriginalName { get; init; }

        /// <summary>
        /// This is the <see cref="OriginalName"/> with MSBuild <c>$(..)</c> vars expanded.
        /// E.g. The expansion might be implemented via ProjectInstance.ExpandString.
        /// </summary>
        public string? ExpandedName { get; init; }

        /// <summary>
        /// This is the <see cref="ExpandedName"/> resolved via <see cref="EnsureProjectFilePath"/>
        /// (i.e., this is a file path if the original text pointed to a directory).
        /// </summary>
        public string? ProjectFilePath { get; init; }

        /// <summary>
        /// Additional item metadata specified as trailing <c>Name=Value</c> pairs,
        /// e.g. <c>#:project ../MyLibrary Private=false</c>.
        /// </summary>
        public ImmutableArray<(string Name, string Value)> Metadata { get; init; }

        public static new Project? Parse(in ParseContext context)
        {
            if (TokenizeWithLegacyFallback(context, nameSeparator: null, allowMetadata: true, out _) is not { } tokens)
            {
                return null;
            }

            if (tokens is not [{ Text.Length: > 0 } firstToken, ..])
            {
                context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, context.DirectiveKind));
                return null;
            }

            if (ParseMetadata(context, tokens, start: 1) is not { IsDefault: false } metadata)
            {
                return null;
            }

            return new Project(context.Info, firstToken.Text) { Metadata = metadata };
        }

        public enum NameKind
        {
            /// <summary>
            /// Change <see cref="Named.Name"/> and <see cref="ExpandedName"/>.
            /// </summary>
            Expanded = 1,

            /// <summary>
            /// Change <see cref="Named.Name"/> and <see cref="Project.ProjectFilePath"/>.
            /// </summary>
            ProjectFilePath = 2,

            /// <summary>
            /// Change only <see cref="Named.Name"/>.
            /// </summary>
            Final = 3,
        }

        public Project WithName(string name, NameKind kind)
        {
            return new Project(Info, name)
            {
                OriginalName = OriginalName,
                ExpandedName = kind == NameKind.Expanded ? name : ExpandedName,
                ProjectFilePath = kind == NameKind.ProjectFilePath ? name : ProjectFilePath,
                Metadata = Metadata,
            };
        }

        /// <summary>
        /// If the directive points to a directory, returns a new directive pointing to the corresponding project file.
        /// </summary>
        public Project EnsureProjectFilePath(ErrorReporter errorReporter)
        {
            var resolvedName = Name;
            var sourcePath = Info.SourceFile.Path;

            // If the path is a directory like '../lib', transform it to a project file path like '../lib/lib.csproj'.
            // Also normalize backslashes to forward slashes to ensure the directive works on all platforms.
            var sourceDirectory = Path.GetDirectoryName(sourcePath)
                ?? throw new InvalidOperationException($"Source file path '{sourcePath}' does not have a containing directory.");

            var resolvedProjectPath = Path.Combine(sourceDirectory, resolvedName.Replace('\\', '/'));
            if (Directory.Exists(resolvedProjectPath))
            {
                if (ProjectLocator.TryGetProjectFileFromDirectory(resolvedProjectPath, out var projectFilePath, out var error))
                {
                    // Keep a relative path only if the original directive was a relative path.
                    resolvedName = ExternalHelpers.IsPathFullyQualified(resolvedName)
                        ? projectFilePath
                        : ExternalHelpers.GetRelativePath(relativeTo: sourceDirectory, projectFilePath);
                }
                else
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidProjectDirective, error));
                }
            }
            else if (!File.Exists(resolvedProjectPath))
            {
                ReportError(string.Format(FileBasedProgramsResources.InvalidProjectDirective, string.Format(FileBasedProgramsResources.CouldNotFindProjectOrDirectory, resolvedProjectPath)));
            }

            return WithName(resolvedName, NameKind.ProjectFilePath);

            void ReportError(string message)
                => errorReporter(Info.SourceFile.Text, sourcePath, Info.Span, message);
        }

        public override string ToString()
        {
            var builder = new StringBuilder("#:project ");
            builder.Append(QuoteIfNeeded(Name));
            AppendMetadata(builder, Metadata);
            return builder.ToString();
        }
    }

    /// <summary>
    /// <c>#:ref</c> directive. References another file-based app as a library.
    /// </summary>
    public sealed class Ref : Named
    {
        public const string ExperimentalFileBasedProgramEnableRefDirective = nameof(ExperimentalFileBasedProgramEnableRefDirective);

        [SetsRequiredMembers]
        public Ref(in ParseInfo info, string name) : base(info)
        {
            Name = name;
            OriginalName = name;
        }

        /// <summary>
        /// Preserved across <see cref="WithName"/> calls, i.e.,
        /// this is the original directive text as entered by the user.
        /// </summary>
        public string OriginalName { get; init; }

        /// <summary>
        /// This is the <see cref="OriginalName"/> with MSBuild <c>$(..)</c> vars expanded.
        /// </summary>
        public string? ExpandedName { get; init; }

        /// <summary>
        /// The resolved full path to the referenced <c>.cs</c> file.
        /// </summary>
        public string? ResolvedPath { get; init; }

        /// <summary>
        /// Additional item metadata specified as trailing <c>Name=Value</c> pairs,
        /// e.g. <c>#:ref ../lib/lib.cs Aliases=lib</c>.
        /// </summary>
        public ImmutableArray<(string Name, string Value)> Metadata { get; init; }

        public static new Ref? Parse(in ParseContext context)
        {
            if (TokenizeWithLegacyFallback(context, nameSeparator: null, allowMetadata: true, out _) is not { } tokens)
            {
                return null;
            }

            if (tokens is not [{ Text.Length: > 0 } firstToken, ..])
            {
                context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, context.DirectiveKind));
                return null;
            }

            if (ParseMetadata(context, tokens, start: 1) is not { IsDefault: false } metadata)
            {
                return null;
            }

            return new Ref(context.Info, firstToken.Text) { Metadata = metadata };
        }

        public enum NameKind
        {
            /// <summary>
            /// Change <see cref="Named.Name"/> and <see cref="ExpandedName"/>.
            /// </summary>
            Expanded = 1,

            /// <summary>
            /// Change <see cref="Named.Name"/> and <see cref="ResolvedPath"/>.
            /// </summary>
            Resolved = 2,

            /// <summary>
            /// Change only <see cref="Named.Name"/>.
            /// </summary>
            Final = 3,
        }

        public Ref WithName(string name, NameKind kind)
        {
            return new Ref(Info, name)
            {
                OriginalName = OriginalName,
                ExpandedName = kind == NameKind.Expanded ? name : ExpandedName,
                ResolvedPath = kind == NameKind.Resolved ? name : ResolvedPath,
                Metadata = Metadata,
            };
        }

        /// <summary>
        /// Resolves the path relative to the source file's directory.
        /// </summary>
        public Ref EnsureResolvedPath(ErrorReporter errorReporter)
        {
            var sourcePath = Info.SourceFile.Path;
            var sourceDirectory = Path.GetDirectoryName(sourcePath)
                ?? throw new InvalidOperationException($"Source file path '{sourcePath}' does not have a containing directory.");

            var resolvedFilePath = Path.GetFullPath(Path.Combine(sourceDirectory, Name.Replace('\\', '/')));

            if (!File.Exists(resolvedFilePath))
            {
                errorReporter(Info.SourceFile.Text, sourcePath, Info.Span,
                    string.Format(FileBasedProgramsResources.InvalidRefDirective,
                        string.Format(FileBasedProgramsResources.CouldNotFindRefFile, resolvedFilePath)));
            }

            return WithName(resolvedFilePath, NameKind.Resolved);
        }

        public override string ToString()
        {
            var builder = new StringBuilder("#:ref ");
            builder.Append(QuoteIfNeeded(Name));
            AppendMetadata(builder, Metadata);
            return builder.ToString();
        }
    }

    public enum IncludeOrExcludeKind
    {
        Include,
        Exclude,
    }

    /// <summary>
    /// <c>#:include</c> or <c>#:exclude</c> directive.
    /// </summary>
    public sealed class IncludeOrExclude(in ParseInfo info) : Named(info)
    {
        public const string MappingPropertyName = "FileBasedProgramsItemMapping";

        public static string DefaultMappingString => ".cs=Compile;.resx=EmbeddedResource;.json=None;.razor=Content;.dll=Reference";

        public static ImmutableArray<(string Extension, string ItemType)> DefaultMapping
        {
            get
            {
                if (field.IsDefault)
                {
                    field =
                    [
                        (".cs", "Compile"),
                        (".resx", "EmbeddedResource"),
                        (".json", "None"),
                        (".razor", "Content"),
                        (".dll", "Reference"),
                    ];
                }

                return field;
            }
        }

        /// <summary>
        /// Preserved across <see cref="WithName"/> calls, i.e.,
        /// this is the original directive text as entered by the user.
        /// </summary>
        public required string OriginalName { get; init; }

        public required IncludeOrExcludeKind Kind { get; init; }

        public string? ItemType { get; init; }

        public static new IncludeOrExclude? Parse(in ParseContext context)
        {
            if (ParseSingleValue(context) is not { } value)
            {
                return null;
            }

            return new IncludeOrExclude(context.Info)
            {
                OriginalName = value,
                Name = value,
                Kind = KindFromString(context.DirectiveKind),
            };
        }

        /// <param name="mapping">
        /// See <see cref="ParseMapping"/>.
        /// </param>
        public IncludeOrExclude WithDeterminedItemType(ErrorReporter reportError, ImmutableArray<(string Extension, string ItemType)> mapping)
        {
            Debug.Assert(ItemType is null);

            string? itemType = null;
            foreach (var entry in mapping)
            {
                if (Name.EndsWith(entry.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    itemType = entry.ItemType;
                    break;
                }
            }

            if (itemType is null)
            {
                reportError(Info.SourceFile.Text, Info.SourceFile.Path, Info.Span,
                    string.Format(FileBasedProgramsResources.IncludeOrExcludeDirectiveUnknownFileType,
                    $"#:{KindToString()}",
                    string.Join(", ", mapping.Select(static e => e.Extension))));
                return this;
            }

            return new IncludeOrExclude(Info)
            {
                OriginalName = OriginalName,
                Name = Name,
                Kind = Kind,
                ItemType = itemType,
            };
        }

        public IncludeOrExclude WithName(string name)
        {
            if (Name == name)
            {
                return this;
            }

            return new IncludeOrExclude(Info)
            {
                OriginalName = OriginalName,
                Name = name,
                Kind = Kind,
                ItemType = ItemType,
            };
        }

        private static IncludeOrExcludeKind KindFromString(string kind)
        {
            return kind switch
            {
                "include" => IncludeOrExcludeKind.Include,
                "exclude" => IncludeOrExcludeKind.Exclude,
                _ => throw new InvalidOperationException($"Unexpected include/exclude directive kind '{kind}'."),
            };
        }

        public override string KindToString()
        {
            return Kind switch
            {
                IncludeOrExcludeKind.Include => "include",
                IncludeOrExcludeKind.Exclude => "exclude",
                _ => throw new InvalidOperationException($"Unexpected {nameof(IncludeOrExcludeKind)} value '{Kind}'."),
            };
        }

        public string KindToMSBuildString()
        {
            return Kind switch
            {
                IncludeOrExcludeKind.Include => "Include",
                IncludeOrExcludeKind.Exclude => "Remove",
                _ => throw new InvalidOperationException($"Unexpected {nameof(IncludeOrExcludeKind)} value '{Kind}'."),
            };
        }

        public override string ToString() => $"#:{KindToString()} {QuoteIfNeeded(Name)}";

        /// <summary>
        /// Parses a <paramref name="value"/> in the format <c>.protobuf=Protobuf;.cshtml=Content</c>.
        /// Should come from MSBuild property with name <see cref="MappingPropertyName"/>.
        /// </summary>
        public static ImmutableArray<(string Extension, string ItemType)> ParseMapping(
            string value,
            SourceFile sourceFile,
            ErrorReporter errorReporter)
        {
            var pairs = value.Split([';'], StringSplitOptions.RemoveEmptyEntries);

            var builder = ImmutableArray.CreateBuilder<(string Extension, string ItemType)>(pairs.Length);

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');

                if (parts.Length != 2)
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidIncludeExcludeMappingEntry, pair));
                    continue;
                }

                var extension = parts[0].Trim();
                var itemType = parts[1].Trim();

                if (extension is not ['.', _, ..])
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidIncludeExcludeMappingExtension, extension, pair));
                    continue;
                }

                if (itemType.IsWhiteSpace())
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidIncludeExcludeMappingItemType, itemType, pair));
                    continue;
                }

                builder.Add((extension, itemType));
            }

            return builder.DrainToImmutable();

            void ReportError(string message)
                => errorReporter(sourceFile.Text, sourceFile.Path, default, message);
        }
    }
}

/// <summary>
/// Detects duplicate directives (by type and case-insensitive name)
/// and reports errors via the provided <see cref="ErrorReporter"/> when their values differ.
/// </summary>
/// <remarks>
/// <c>#:project</c>, <c>#:ref</c>, <c>#:include</c>, and <c>#:exclude</c> duplicates are allowed (MSBuild can handle them).
/// </remarks>
internal struct DirectiveDeduplicator
{
    private Dictionary<CSharpDirective.Named, CSharpDirective.Named>? _seen;

    /// <summary>
    /// Checks <paramref name="directive"/> for duplication and reports an error if a different unevaluated value was already seen.
    /// </summary>
    /// <param name="shouldKeep"><see langword="false"/> if a duplicate directive was already seen and this directive should be skipped.</param>
    public void CheckDirective(CSharpDirective.Named directive, ErrorReporter reportError, out bool shouldKeep)
    {
        if (directive is CSharpDirective.Project or CSharpDirective.Ref or CSharpDirective.IncludeOrExclude)
        {
            shouldKeep = true;
            return;
        }

        _seen ??= new(NamedDirectiveComparer.Instance);

        if (_seen.TryGetValue(directive, out var existingDirective))
        {
            if (HasSameValue(existingDirective, directive))
            {
                shouldKeep = false;
                return;
            }

            var typeAndName = $"#:{existingDirective.KindToString()} {existingDirective.Name}";
            reportError(directive.Info.SourceFile.Text, directive.Info.SourceFile.Path, directive.Info.Span,
                string.Format(FileBasedProgramsResources.DuplicateDirective, typeAndName));

            shouldKeep = false;
            return;
        }
        else
        {
            _seen.Add(directive, directive);
        }

        shouldKeep = true;
    }

    private static bool HasSameValue(CSharpDirective.Named existingDirective, CSharpDirective.Named directive)
    {
        Debug.Assert(NamedDirectiveComparer.Instance.Equals(existingDirective, directive));
        Debug.Assert(existingDirective is CSharpDirective.Sdk or CSharpDirective.Property or CSharpDirective.Package);
        Debug.Assert(directive is CSharpDirective.Sdk or CSharpDirective.Property or CSharpDirective.Package);

        return (existingDirective, directive) switch
        {
            (CSharpDirective.Sdk existing, CSharpDirective.Sdk current) =>
                string.Equals(existing.Version, current.Version, StringComparison.Ordinal),
            (CSharpDirective.Property existing, CSharpDirective.Property current) =>
                string.Equals(existing.Value, current.Value, StringComparison.Ordinal),
            (CSharpDirective.Package existing, CSharpDirective.Package current) =>
                string.Equals(existing.Version, current.Version, StringComparison.Ordinal) &&
                HasSameMetadata(existing.Metadata, current.Metadata),
            _ => false,
        };
    }

    private static bool HasSameMetadata(
        ImmutableArray<(string Name, string Value)> existingMetadata,
        ImmutableArray<(string Name, string Value)> currentMetadata)
    {
        if (existingMetadata.Length != currentMetadata.Length)
        {
            return false;
        }

        for (var i = 0; i < existingMetadata.Length; i++)
        {
            var existing = existingMetadata[i];
            var current = currentMetadata[i];
            if (!CSharpDirective.MetadataNameComparer.Equals(existing.Name, current.Name) ||
                !CSharpDirective.MetadataValueComparer.Equals(existing.Value, current.Value))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Used for deduplication - compares directives by their type and name (ignoring case).
/// </summary>
internal sealed class NamedDirectiveComparer : IEqualityComparer<CSharpDirective.Named>
{
    public static readonly NamedDirectiveComparer Instance = new();

    private NamedDirectiveComparer() { }

    public bool Equals(CSharpDirective.Named? x, CSharpDirective.Named? y)
    {
        if (ReferenceEquals(x, y)) return true;

        if (x is null || y is null) return false;

        return x.GetType() == y.GetType() &&
            StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);
    }

    public int GetHashCode(CSharpDirective.Named obj)
    {
        return ExternalHelpers.CombineHashCodes(
            obj.GetType().GetHashCode(),
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
    }
}

internal sealed class SimpleDiagnostic
{
    public required Position Location { get; init; }
    public required string Message { get; init; }

    /// <summary>
    /// An adapter of <see cref="FileLinePositionSpan"/> that ensures we JSON-serialize only the necessary fields.
    /// </summary>
    /// <remarks>
    /// note: this type is only serialized for run-api scenarios.
    /// If/when run-api is removed, we would also want to remove the usage of System.Text.Json attributes.
    /// </remarks>
    public readonly struct Position
    {
        public required string Path { get; init; }
        public required LinePositionSpan Span { get; init; }
#if FILE_BASED_PROGRAMS_SYSTEM_TEXT_JSON // only run-api needs this, see remarks
        [System.Text.Json.Serialization.JsonIgnore]
#endif
        public TextSpan TextSpan { get; init; }
    }
}

internal delegate void ErrorReporter(SourceText text, string path, TextSpan textSpan, string message, Exception? innerException = null);

internal static partial class ErrorReporters
{
    public static readonly ErrorReporter IgnoringReporter =
        static (_, _, _, _, _) => { };

    public static ErrorReporter CreateCollectingReporter(out ImmutableArray<SimpleDiagnostic>.Builder builder)
    {
        var capturedBuilder = builder = ImmutableArray.CreateBuilder<SimpleDiagnostic>();

        return (text, path, textSpan, message, _) =>
            capturedBuilder.Add(new SimpleDiagnostic
            {
                Location = new SimpleDiagnostic.Position()
                {
                    Path = path,
                    TextSpan = textSpan,
                    Span = text.Lines.GetLinePositionSpan(textSpan)
                },
                Message = message
            });
    }
}
