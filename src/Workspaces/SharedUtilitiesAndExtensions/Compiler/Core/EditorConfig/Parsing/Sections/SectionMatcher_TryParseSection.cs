// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing;

internal readonly partial struct SectionMatcher
{
    public static bool TryParseSection(string headerText, out SectionMatcher matcher)
    {
        // An editorconfig section name is a language for recognizing file paths
        // defined by the following grammar:
        //
        // <path> ::= <path-list>
        // <path-list> ::= <path-item> | <path-item> <path-list>
        // <path-item> ::= "*"  | "**" | "?" | <char> | <choice> | <range>
        // <char> ::= any unicode character
        // <choice> ::= "{" <choice-list> "}"
        // <choice-list> ::= <path-list> | <path-list> "," <choice-list>
        // <range> ::= "{" <integer> ".." <integer> "}"
        // <integer> ::= "-" <digit-list> | <digit-list>
        // <digit-list> ::= <digit> | <digit> <digit-list>
        // <digit> ::= 0-9

        matcher = default;
        using var _0 = PooledStringBuilder.GetInstance(out var sb);
        sb.Append('^');

        // EditorConfig matching depends on the whether or not there are
        // directory separators and where they are located in the section
        // name. Specifically, the editorconfig core parser says:
        // https://github.com/editorconfig/editorconfig-core-c/blob/5d3996811e962a717a7d7fdd0a941192382241a7/src/lib/editorconfig.c#L231
        //
        //     Pattern would be:
        //     /dir/of/editorconfig/file[double_star]/[section] if section does not contain '/',
        //     /dir/of/editorconfig/file[section] if section starts with a '/', or
        //     /dir/of/editorconfig/file/[section] if section contains '/' but does not start with '/'.

        if (!headerText.Contains("/"))
        {
            sb.Append(".*/");
        }
        else if (headerText[0] != '/')
        {
            sb.Append('/');
        }

        var lexer = new Lexer(headerText);
        using var _1 = ArrayBuilder<(int minValue, int maxValue)>.GetInstance(out var numberRangePairs);
        if (!TryCompilePathList(ref lexer, sb, parsingChoice: false, numberRangePairs))
        {
            return false;
        }

        sb.Append('$');
        var pattern = sb.ToString();
        matcher = new SectionMatcher(new Regex(pattern), headerText, numberRangePairs.ToImmutableArray());
        return true;
    }

    private static bool TryCompilePathList(
        ref Lexer lexer,
        StringBuilder sb,
        bool parsingChoice,
        ArrayBuilder<(int minValue, int maxValue)> numberRangePairs)
    {
        while (!lexer.IsDone)
        {
            var tokenKind = lexer.Lex();
            switch (tokenKind)
            {
                case TokenKind.BadToken:
                    // Parsing failure
                    return false;
                case TokenKind.SimpleCharacter:
                    // Matches just this character
                    sb.Append(Regex.Escape(lexer.EatCurrentCharacter().ToString()));
                    break;
                case TokenKind.Question:
                    // '?' matches any single character
                    sb.Append('.');
                    break;
                case TokenKind.Star:
                    // Matches any string of characters except directory separator
                    // Directory separator is defined in editorconfig spec as '/'
                    sb.Append("[^/]*");
                    break;
                case TokenKind.StarStar:
                    // Matches any string of characters
                    sb.Append(".*");
                    break;
                case TokenKind.OpenCurly:
                    // Back up token stream. The following helpers all expect a '{'
                    lexer.Position--;
                    // This is ambiguous between {num..num} and {item1,item2}
                    // We need to look ahead to disambiguate. Looking for {num..num}
                    // is easier because it can't be recursive.
                    var numberRange = TryParseNumberRange(ref lexer);
                    if (numberRange is null)
                    {
                        // Not a number range. Try a choice expression
                        if (!TryCompileChoice(ref lexer, sb, numberRangePairs))
                        {
                            return false;
                        }
                        // Keep looping. There may be more after the '}'.
                        break;
                    }
                    else
                    {
                        var (numStart, numEnd) = numberRange.Value;
                        if (int.TryParse(numStart, out var intStart) && int.TryParse(numEnd, out var intEnd))
                        {
                            var pair = intStart < intEnd ? (intStart, intEnd) : (intEnd, intStart);
                            numberRangePairs.Add(pair);
                            // Group allowing any digit sequence. The validity will be checked outside of the regex
                            sb.Append("(-?[0-9]+)");
                            // Keep looping
                            break;
                        }

                        return false;
                    }
                case TokenKind.CloseCurly:
                    // Either the end of a choice, or a failed parse
                    return parsingChoice;
                case TokenKind.Comma:
                    // The end of a choice section, or a failed parse
                    return parsingChoice;
                case TokenKind.OpenBracket:
                    sb.Append('[');
                    if (!TryCompileCharacterClass(ref lexer, sb))
                    {
                        return false;
                    }

                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(tokenKind);
            }
        }
        // If we're parsing a choice we should not exit without a closing '}'
        return !parsingChoice;
    }

    private static (string numStart, string numEnd)? TryParseNumberRange(ref Lexer lexer)
    {
        var saved = lexer.Position;
        if (lexer.Lex() != TokenKind.OpenCurly)
        {
            lexer.Position = saved;
            return null;
        }

        var numStart = lexer.TryLexNumber();
        if (numStart is null)
        {
            // Not a number
            lexer.Position = saved;
            return null;
        }

        // The next two characters must be ".."
        if (!lexer.TryEatCurrentCharacter(out var c) || c != '.' ||
            !lexer.TryEatCurrentCharacter(out c) || c != '.')
        {
            lexer.Position = saved;
            return null;
        }

        // Now another number
        var numEnd = lexer.TryLexNumber();
        if (numEnd is null || lexer.IsDone || lexer.Lex() != TokenKind.CloseCurly)
        {
            // Not a number or no '}'
            lexer.Position = saved;
            return null;
        }

        return (numStart, numEnd);
    }

    private static bool TryCompileCharacterClass(ref Lexer lexer, StringBuilder sb)
    {
        // [...] should match any of the characters in the brackets, with special
        // behavior for four characters: '!' immediately after the opening bracket
        // implies the negation of the character class, '-' implies matching
        // between the locale-dependent range of the previous and next characters,
        // '\' escapes the following character, and ']' ends the range
        if (!lexer.IsDone && lexer.CurrentCharacter == '!')
        {
            sb.Append('^');
            lexer.Position++;
        }

        while (!lexer.IsDone)
        {
            var currentChar = lexer.EatCurrentCharacter();
            switch (currentChar)
            {
                case '-':
                    // '-' means the same thing in regex as it does in the glob, so
                    // put it in verbatim
                    sb.Append(currentChar);
                    break;

                case '\\':
                    // Escape the next char
                    if (lexer.IsDone)
                    {
                        return false;
                    }

                    sb.Append('\\');
                    sb.Append(lexer.EatCurrentCharacter());
                    break;

                case ']':
                    sb.Append(currentChar);
                    return true;

                default:
                    sb.Append(Regex.Escape(currentChar.ToString()));
                    break;
            }
        }
        // Stream ended without a closing bracket
        return false;
    }

    private static bool TryCompileChoice(
        ref Lexer lexer,
        StringBuilder sb,
        ArrayBuilder<(int, int)> numberRangePairs)
    {
        if (lexer.Lex() != TokenKind.OpenCurly)
        {
            return false;
        }

        // Start a non-capturing group for the choice
        sb.Append("(?:");

        // We start immediately after a '{'
        // Try to compile the nested <path-list>
        while (TryCompilePathList(ref lexer, sb, parsingChoice: true, numberRangePairs))
        {
            // If we've successfully compiled a <path-list> the last token should
            // have been a ',' or a '}'
            var lastChar = lexer[lexer.Position - 1];
            if (lastChar == ',')
            {
                // Another option
                sb.Append('|');
            }
            else if (lastChar == '}')
            {
                // Close out the capture group
                sb.Append(')');
                return true;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(lastChar);
            }
        }

        // Propagate failure
        return false;
    }
}
