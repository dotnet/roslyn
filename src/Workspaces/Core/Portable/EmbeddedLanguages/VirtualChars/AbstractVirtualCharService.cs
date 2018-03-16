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
        protected abstract ImmutableArray<VirtualChar> TryConvertToVirtualCharsWorker(SyntaxToken token);

        public ImmutableArray<VirtualChar> TryConvertToVirtualChars(SyntaxToken token)
        {
            // We don't process any strings that contain diagnostics in it.  That means that we can 
            // trust that all the string's contents (most importantly, the escape sequences) are well
            // formed.
            if (token.ContainsDiagnostics)
            {
                return default;
            }

            var result = TryConvertToVirtualCharsWorker(token);

#if DEBUG
            // Do some invariant checking to make sure we processed the string token the same
            // way the C# and VB compilers did.
            if (!result.IsDefault)
            {
                // Ensure that we properly broke up the token into a sequence of characters that
                // matches what the compiler did.
                var expectedValueText = token.ValueText;
                var actualValueText = result.CreateString();
                Debug.Assert(expectedValueText == actualValueText);

                if (result.Length > 0)
                {
                    var currentVC = result[0];
                    Debug.Assert(currentVC.Span.Start > token.SpanStart, "First span has to start after the start of the string token (including its delimeter)");
                    Debug.Assert(currentVC.Span.Start == token.SpanStart + 1 || currentVC.Span.Start == token.SpanStart + 2, "First span should start on the second or third char of the string.");
                    for (var i = 1; i < result.Length; i++)
                    {
                        var nextVC = result[i];
                        Debug.Assert(currentVC.Span.End == nextVC.Span.Start, "Virtual character spans have to be touching.");
                        currentVC = nextVC;
                    }

                    var lastVC = result.Last();
                    Debug.Assert(lastVC.Span.End == token.Span.End - 1, "Last span has to end right before the end of hte string token (including its trailing delimeter).");
                }
            }
#endif

            return result;
        }

        /// <summary>
        /// Helper to convert simple string literals that escape quotes by doubling them.  This is 
        /// how normal VB literals and c# verbatim string literals work.
        /// </summary>
        protected ImmutableArray<VirtualChar> TryConvertSimpleDoubleQuoteString(
            SyntaxToken token, string startDelimeter, string endDelimeter)
        {
            Debug.Assert(!token.ContainsDiagnostics);

            var tokenText = token.Text;
            if (!tokenText.StartsWith(startDelimeter) ||
                !tokenText.EndsWith(endDelimeter))
            {
                Debug.Assert(false, "This should not be reachable as long as the compiler added no diagnostics.");
                return default;
            }

            var startIndexInclusive = startDelimeter.Length;
            var endIndexExclusive = tokenText.Length - endDelimeter.Length;

            var result = ArrayBuilder<VirtualChar>.GetInstance();

            var offset = token.SpanStart;
            for (var index = startIndexInclusive; index < endIndexExclusive;)
            {
                if (tokenText[index] == '"' &&
                    tokenText[index + 1] == '"')
                {
                    result.Add(new VirtualChar('"', new TextSpan(offset + index, 2)));
                    index += 2;
                }
                else
                {
                    result.Add(new VirtualChar(tokenText[index], new TextSpan(offset + index, 1)));
                    index++;
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}
