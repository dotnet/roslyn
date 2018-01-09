// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.VirtualChars
{
    internal abstract class AbstractVirtualCharService : IVirtualCharService
    {
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

        protected abstract ImmutableArray<VirtualChar> TryConvertToVirtualCharsWorker(SyntaxToken token);
    }
}
