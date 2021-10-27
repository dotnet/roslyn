// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;

    internal partial struct StackFrameParser
    {
        /// <summary>
        /// Exception type for when parsing encounters an exceptional state and must halt. This varies based on input,
        /// but some examples are
        /// * Open type arguments without closing
        /// * Missing required identifier (such as parameter with type but no identifier name)
        /// * Missing close bracket on array type
        /// </summary>
        private class StackFrameParseException : Exception
        {
            public StackFrameParseException(StackFrameKind expectedKind, StackFrameNodeOrToken actual)
                : this($"Expected {expectedKind} instead of {GetDetails(actual)}")
            {
            }

            private static string GetDetails(StackFrameNodeOrToken actual)
            {
                if (actual.IsNode)
                {
                    var node = actual.Node;
                    return $"'{node.Kind}' at {node.GetSpan().Start}";
                }
                else
                {
                    var token = actual.Token;
                    return $"'{token.VirtualChars.CreateString()}' at {token.GetSpan().Start}";
                }
            }

            public StackFrameParseException(string message)
                : base(message)
            {
            }
        }
    }
}
