// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0240 // "redundant nullable directive" - this file is source-shared
#nullable enable
#pragma warning restore IDE0240

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.FileBasedPrograms;

internal sealed class LanguageService : ILanguageService
{
    public static LanguageService Instance { get; } = new();

    public static SyntaxTokenParser CreateTokenizer(SourceText text)
    {
        return SyntaxFactory.CreateTokenParser(text,
            CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]));
    }

    public ImmutableArray<CSharpDirective> FindDirectives(SourceFile sourceFile, bool reportAllErrors, ErrorReporter errorReporter, bool checkDuplicates = true)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpDirective>();
        using var tokenizer = CreateTokenizer(sourceFile.Text);

        var result = tokenizer.ParseLeadingTrivia();
        var triviaList = result.Token.LeadingTrivia;

        FindLeadingDirectives(sourceFile, triviaList, errorReporter, builder, checkDuplicates);

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
        bool checkDuplicates = true)
    {
        var deduplicator = new DirectiveDeduplicator();
        TextSpan previousWhiteSpaceSpan = default;

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

                var message = trivia.GetStructure() is IgnoredDirectiveTriviaSyntax { Content: { RawKind: (int)SyntaxKind.StringLiteralToken } content }
                    ? content.Text.AsSpan().Trim()
                    : "";
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
                };

                // Block quotes now so we can later support quoted values without a breaking change. https://github.com/dotnet/sdk/issues/49367
                if (value.Contains("\""))
                {
                    context.ReportError(FileBasedProgramsResources.QuoteInDirective);
                }

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
