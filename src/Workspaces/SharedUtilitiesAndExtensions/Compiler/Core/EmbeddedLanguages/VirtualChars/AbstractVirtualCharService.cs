// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract class AbstractVirtualCharService : IVirtualCharService
    {
        public abstract bool TryGetEscapeCharacter(VirtualChar ch, out char escapedChar);

        protected abstract bool IsStringOrCharLiteralToken(SyntaxToken token);
        protected abstract VirtualCharSequence TryConvertToVirtualCharsWorker(SyntaxToken token);

        /// <summary>
        /// Returns <see langword="true"/> if the next two characters at <c>tokenText[index]</c> are <c>{{</c> or
        /// <c>}}</c>.  If so, <paramref name="span"/> will contain the span of those two characters (based on <paramref
        /// name="tokenText"/> starting at <paramref name="offset"/>).
        /// </summary>
        protected static bool IsLegalBraceEscape(
            string tokenText, int index, int offset, out TextSpan span)
        {
            if (index + 1 < tokenText.Length)
            {
                var ch = tokenText[index];
                var next = tokenText[index + 1];
                if ((ch == '{' && next == '{') ||
                    (ch == '}' && next == '}'))
                {
                    span = new TextSpan(offset + index, 2);
                    return true;
                }
            }

            span = default;
            return false;
        }

        public VirtualCharSequence TryConvertToVirtualChars(SyntaxToken token)
        {
            // We don't process any strings that contain diagnostics in it.  That means that we can 
            // trust that all the string's contents (most importantly, the escape sequences) are well
            // formed.
            if (token.ContainsDiagnostics)
            {
                return default;
            }

            var result = TryConvertToVirtualCharsWorker(token);
            CheckInvariants(token, result);

            return result;
        }

        [Conditional("DEBUG")]
        private void CheckInvariants(SyntaxToken token, VirtualCharSequence result)
        {
            // Do some invariant checking to make sure we processed the string token the same
            // way the C# and VB compilers did.
            if (!result.IsDefault)
            {
                // Ensure that we properly broke up the token into a sequence of characters that
                // matches what the compiler did.
                if (IsStringOrCharLiteralToken(token))
                {
                    var expectedValueText = token.ValueText;
                    var actualValueText = result.CreateString();
                    Debug.Assert(expectedValueText == actualValueText);
                }

                if (result.Length > 0)
                {
                    var currentVC = result[0];
                    Debug.Assert(currentVC.Span.Start >= token.SpanStart, "First span has to start after the start of the string token");
                    if (IsStringOrCharLiteralToken(token))
                    {
                        Debug.Assert(currentVC.Span.Start == token.SpanStart + 1 ||
                                     currentVC.Span.Start == token.SpanStart + 2, "First span should start on the second or third char of the string.");
                    }
                    else
                    {
                        Debug.Assert(currentVC.Span.Start == token.SpanStart, "First span should start on the first char of the string.");
                    }

                    for (var i = 1; i < result.Length; i++)
                    {
                        var nextVC = result[i];
                        Debug.Assert(currentVC.Span.End == nextVC.Span.Start, "Virtual character spans have to be touching.");
                        currentVC = nextVC;
                    }

                    var lastVC = result.Last();

                    if (IsStringOrCharLiteralToken(token))
                    {
                        Debug.Assert(lastVC.Span.End == token.Span.End - 1, "Last span has to end right before the end of the string token.");
                    }
                    else
                    {
                        Debug.Assert(lastVC.Span.End == token.Span.End, "Last span has to end right before the end of the string token.");
                    }
                }
            }
        }

        /// <summary>
        /// Helper to convert simple string literals that escape quotes by doubling them.  This is 
        /// how normal VB literals and c# verbatim string literals work.
        /// </summary>
        /// <param name="startDelimiter">The start characters string.  " in VB and @" in C#</param>
        protected static VirtualCharSequence TryConvertSimpleDoubleQuoteString(
            SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
        {
            Debug.Assert(!token.ContainsDiagnostics);

            if (escapeBraces)
            {
                Debug.Assert(startDelimiter == "");
                Debug.Assert(endDelimiter == "");
            }

            var tokenText = token.Text;

            if (startDelimiter.Length > 0 && !tokenText.StartsWith(startDelimiter))
            {
                Debug.Assert(false, "This should not be reachable as long as the compiler added no diagnostics.");
                return default;
            }

            if (endDelimiter.Length > 0 && !tokenText.EndsWith(endDelimiter))
            {
                Debug.Assert(false, "This should not be reachable as long as the compiler added no diagnostics.");
                return default;
            }

            var startIndexInclusive = startDelimiter.Length;
            var endIndexExclusive = tokenText.Length - endDelimiter.Length;

            using var _ = ArrayBuilder<VirtualChar>.GetInstance(out var result);
            var offset = token.SpanStart;

            for (var index = startIndexInclusive; index < endIndexExclusive;)
            {
                if (tokenText[index] == '"' && tokenText[index + 1] == '"')
                {
                    result.Add(VirtualChar.Create(new Rune('"'), new TextSpan(offset + index, 2)));
                    index += 2;
                }
                else if (escapeBraces && IsOpenOrCloseBrace(tokenText[index]))
                {
                    if (!IsLegalBraceEscape(tokenText, index, offset, out var span))
                        return default;

                    result.Add(VirtualChar.Create(new Rune(tokenText[index]), span));
                    index += result.Last().Span.Length;
                }
                else if (Rune.TryCreate(tokenText[index], out var rune))
                {
                    // First, see if this was a single char that can become a rune (the common case).
                    result.Add(VirtualChar.Create(rune, new TextSpan(offset + index, 1)));
                    index += 1;
                }
                else if (index + 1 < tokenText.Length &&
                         Rune.TryCreate(tokenText[index], tokenText[index + 1], out rune))
                {
                    // Otherwise, see if we have a surrogate pair (less common, but possible).
                    result.Add(VirtualChar.Create(rune, new TextSpan(offset + index, 2)));
                    index += 2;
                }
                else
                {
                    // Something that couldn't be encoded as runes.
                    Debug.Assert(char.IsSurrogate(tokenText[index]));
                    result.Add(VirtualChar.Create(tokenText[index], new TextSpan(offset + index, 1)));
                    index += 1;
                }
            }

            return CreateVirtualCharSequence(
                tokenText, offset, startIndexInclusive, endIndexExclusive, result);
        }

        protected static bool IsOpenOrCloseBrace(char ch)
            => ch == '{' || ch == '}';

        protected static VirtualCharSequence CreateVirtualCharSequence(
            string tokenText, int offset,
            int startIndexInclusive, int endIndexExclusive,
            ArrayBuilder<VirtualChar> result)
        {
            // Check if we actually needed to create any special virtual chars.
            // if not, we can avoid the entire array allocation and just wrap
            // the text of the token and pass that back.

            var textLength = endIndexExclusive - startIndexInclusive;
            if (textLength == result.Count)
            {
                var sequence = VirtualCharSequence.Create(offset, tokenText);
                return sequence.GetSubSequence(TextSpan.FromBounds(startIndexInclusive, endIndexExclusive));
            }

            return VirtualCharSequence.Create(result.ToImmutable());
        }
    }
}
