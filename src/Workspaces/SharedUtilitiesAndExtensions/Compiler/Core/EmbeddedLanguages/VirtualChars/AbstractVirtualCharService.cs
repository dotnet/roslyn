// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

internal abstract partial class AbstractVirtualCharService : IVirtualCharService
{
    public abstract bool TryGetEscapeCharacter(VirtualChar ch, out char escapedChar);

    protected abstract ISyntaxFacts SyntaxFacts { get; }

    protected abstract VirtualCharGreenSequence TryConvertToVirtualCharsWorker(SyntaxToken token);
    protected abstract bool IsMultiLineRawStringToken(SyntaxToken token);

    /// <summary>
    /// Returns <see langword="true"/> if the next two characters at <c>tokenText[index]</c> are <c>{{</c> or
    /// <c>}}</c>.
    /// </summary>
    protected static bool IsLegalBraceEscape(
        string tokenText, int index, out int width)
    {
        if (index + 1 < tokenText.Length)
        {
            var ch = tokenText[index];
            var next = tokenText[index + 1];
            if ((ch == '{' && next == '{') ||
                (ch == '}' && next == '}'))
            {
                width = 2;
                return true;
            }
        }

        width = 0;
        return false;
    }

    public VirtualCharSequence TryConvertToVirtualChars(SyntaxToken token)
    {
        // We don't process any strings that contain diagnostics in it.  That means that we can 
        // trust that all the string's contents (most importantly, the escape sequences) are well
        // formed.
        if (token.ContainsDiagnostics)
            return default;

        var greenSequence = TryConvertToVirtualCharsWorker(token);
        var result = new VirtualCharSequence(token.SpanStart, greenSequence);
        CheckInvariants(token, result);

        return result;
    }

    [Conditional("DEBUG")]
    private void CheckInvariants(SyntaxToken token, VirtualCharSequence result)
    {
        // Do some invariant checking to make sure we processed the string token the same
        // way the C# and VB compilers did.
        if (result.IsDefault)
            return;

        // Ensure that we properly broke up the token into a sequence of characters that matches what the compiler did.
        // Note: we don't do this for all syntaxKinds.  For example an InterpolatedStringTextToken does not do the
        // ValueText processing that a StringLiteralToken does.  So, for example, $"{{" will have a ValueText of "{{"
        // not "{" which might otherwise be expected.
        var syntaxKinds = this.SyntaxFacts.SyntaxKinds;
        if (token.RawKind == syntaxKinds.StringLiteralToken ||
            token.RawKind == syntaxKinds.Utf8StringLiteralToken ||
            token.RawKind == syntaxKinds.CharacterLiteralToken)
        {
            var expectedValueText = token.ValueText;
            var actualValueText = result.CreateString();
            Debug.Assert(expectedValueText == actualValueText);
        }

        if (result.Length > 0)
        {
            var currentVC = result[0];
            Debug.Assert(currentVC.Span.Start >= token.SpanStart, "First span has to start after the start of the string token");
            if (token.RawKind == syntaxKinds.StringLiteralToken ||
                token.RawKind == syntaxKinds.CharacterLiteralToken)
            {
                Debug.Assert(currentVC.Span.Start == token.SpanStart + 1 ||
                                currentVC.Span.Start == token.SpanStart + 2, "First span should start on the second or third char of the string.");
            }

            if (IsMultiLineRawStringToken(token))
            {
                for (var i = 1; i < result.Length; i++)
                {
                    var nextVC = result[i];
                    Debug.Assert(currentVC.Span.End <= nextVC.Span.Start, "Virtual character spans have to be ordered.");
                    currentVC = nextVC;
                }
            }
            else
            {
                for (var i = 1; i < result.Length; i++)
                {
                    var nextVC = result[i];
                    Debug.Assert(currentVC.Span.End == nextVC.Span.Start, "Virtual character spans have to be touching.");
                    currentVC = nextVC;
                }
            }

            var lastVC = result[^1];

            if (token.RawKind == syntaxKinds.StringLiteralToken ||
                token.RawKind == syntaxKinds.CharacterLiteralToken)
            {
                Debug.Assert(lastVC.Span.End == token.Span.End - "\"".Length, "Last span has to end right before the end of the string token.");
            }
            else if (token.RawKind == syntaxKinds.Utf8StringLiteralToken)
            {
                Debug.Assert(lastVC.Span.End == token.Span.End - "\"u8".Length, "Last span has to end right before the end of the string token.");
            }
        }
    }

    /// <summary>
    /// Helper to convert simple string literals that escape quotes by doubling them.  This is 
    /// how normal VB literals and c# verbatim string literals work.
    /// </summary>
    /// <param name="startDelimiter">The start characters string.  " in VB and @" in C#</param>
    protected static VirtualCharGreenSequence TryConvertSimpleDoubleQuoteString(
        SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
    {
        Debug.Assert(!token.ContainsDiagnostics);

        if (escapeBraces)
        {
            Debug.Assert(startDelimiter == "");
            Debug.Assert(endDelimiter == "");
        }

        var tokenText = token.Text;

        if (startDelimiter.Length > 0 && !tokenText.StartsWith(startDelimiter, StringComparison.Ordinal))
        {
            Debug.Assert(false, "This should not be reachable as long as the compiler added no diagnostics.");
            return default;
        }

        if (endDelimiter.Length > 0 && !tokenText.EndsWith(endDelimiter, StringComparison.Ordinal))
        {
            Debug.Assert(false, "This should not be reachable as long as the compiler added no diagnostics.");
            return default;
        }

        var startIndexInclusive = startDelimiter.Length;
        var endIndexExclusive = tokenText.Length - endDelimiter.Length;

        var result = ImmutableSegmentedList.CreateBuilder<VirtualCharGreen>();

        for (var index = startIndexInclusive; index < endIndexExclusive;)
        {
            if (tokenText[index] == '"' && tokenText[index + 1] == '"')
            {
                result.Add(new VirtualCharGreen('"', offset: index, width: 2));
                index += 2;
                continue;
            }
            else if (escapeBraces && IsOpenOrCloseBrace(tokenText[index]))
            {
                if (!IsLegalBraceEscape(tokenText, index, out var width))
                    return default;

                result.Add(new VirtualCharGreen(tokenText[index], offset: index, width: width));
                index += width;
                continue;
            }

            index += ConvertTextAtIndexToVirtualChar(tokenText, index, result);
        }

        return CreateVirtualCharSequence(
            tokenText, startIndexInclusive, endIndexExclusive, result);
    }

    /// <summary>
    /// Returns the number of characters to jump forward (either 1 or 2);
    /// </summary>
    protected static int ConvertTextAtIndexToVirtualChar(string tokenText, int index, ImmutableSegmentedList<VirtualCharGreen>.Builder result)
        => ConvertTextAtIndexToVirtualChar(tokenText, index, new StringTextInfo(), result);

    protected static int ConvertTextAtIndexToVirtualChar(SourceText tokenText, int index, ImmutableSegmentedList<VirtualCharGreen>.Builder result)
        => ConvertTextAtIndexToVirtualChar(tokenText, index, new SourceTextTextInfo(), result);

    private static int ConvertTextAtIndexToVirtualChar<T, TTextInfo>(
        T tokenText, int index, TTextInfo info, ImmutableSegmentedList<VirtualCharGreen>.Builder result)
        where TTextInfo : struct, ITextInfo<T>
    {
        var ch = info.Get(tokenText, index);
        result.Add(new VirtualCharGreen(ch, offset: index, width: 1));
        return 1;
    }

    protected static bool IsOpenOrCloseBrace(char ch)
        => ch is '{' or '}';

    protected static VirtualCharGreenSequence CreateVirtualCharSequence(
        string tokenText,
        int startIndexInclusive,
        int endIndexExclusive,
        ImmutableSegmentedList<VirtualCharGreen>.Builder result)
    {
        // Check if we actually needed to create any special virtual chars.
        // if not, we can avoid the entire array allocation and just wrap
        // the text of the token and pass that back.

        var textLength = endIndexExclusive - startIndexInclusive;
        if (textLength == result.Count)
        {
            var sequence = VirtualCharGreenSequence.Create(tokenText);
            return sequence[startIndexInclusive..endIndexExclusive];
        }

        return VirtualCharGreenSequence.Create(result.ToImmutable());
    }
}
