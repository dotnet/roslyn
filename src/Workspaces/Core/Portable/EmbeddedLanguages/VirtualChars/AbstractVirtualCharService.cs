// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract class AbstractVirtualCharService : IVirtualCharService
    {
        protected abstract bool IsStringLiteralToken(SyntaxToken token);
        protected abstract VirtualCharSequence TryConvertToVirtualCharsWorker(SyntaxToken token);

        protected static bool TryAddBraceEscape(
            ArrayBuilder<VirtualChar> result, string tokenText, int offset, int index)
        {
            if (index + 1 < tokenText.Length)
            {
                var ch = tokenText[index];
                var next = tokenText[index + 1];
                if ((ch == '{' && next == '{') ||
                    (ch == '}' && next == '}'))
                {
                    result.Add(new VirtualChar(ch, new TextSpan(offset + index, 2)));
                    return true;
                }
            }

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
                if (IsStringLiteralToken(token))
                {
                    var expectedValueText = token.ValueText;
                    var actualValueText = result.CreateString();
                    Debug.Assert(expectedValueText == actualValueText);
                }

                if (result.Length > 0)
                {
                    var currentVC = result[0];
                    Debug.Assert(currentVC.Span.Start >= token.SpanStart, "First span has to start after the start of the string token");
                    if (IsStringLiteralToken(token))
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

                    if (IsStringLiteralToken(token))
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

            var result = ArrayBuilder<VirtualChar>.GetInstance();
            var offset = token.SpanStart;
            try
            {
                for (var index = startIndexInclusive; index < endIndexExclusive;)
                {
                    if (tokenText[index] == '"' &&
                        tokenText[index + 1] == '"')
                    {
                        result.Add(new VirtualChar('"', new TextSpan(offset + index, 2)));
                        index += 2;
                    }
                    else if (escapeBraces &&
                             (tokenText[index] == '{' || tokenText[index] == '}'))
                    {
                        if (!TryAddBraceEscape(result, tokenText, offset, index))
                        {
                            return default;
                        }

                        index += result.Last().Span.Length;
                    }
                    else
                    {
                        result.Add(new VirtualChar(tokenText[index], new TextSpan(offset + index, 1)));
                        index++;
                    }
                }

                return CreateVirtualCharSequence(
                    tokenText, offset, startIndexInclusive, endIndexExclusive, result);
            }
            finally
            {
                result.Free();
            }
        }

        protected static VirtualCharSequence CreateVirtualCharSequence(
            string tokenText, int offset, int startIndexInclusive, int endIndexExclusive, ArrayBuilder<VirtualChar> result)
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
