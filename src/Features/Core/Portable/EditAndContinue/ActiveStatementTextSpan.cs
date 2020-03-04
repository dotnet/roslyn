// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementTextSpan
    {
        public readonly ActiveStatementFlags Flags;
        public readonly TextSpan Span;

        public ActiveStatementTextSpan(ActiveStatementFlags flags, TextSpan span)
        {
            Flags = flags;
            Span = span;
        }

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a leaf frame.
        /// </summary>
        public bool IsLeaf => (Flags & ActiveStatementFlags.IsLeafFrame) != 0;

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a non-leaf frame.
        /// </summary>
        public bool IsNonLeaf => (Flags & ActiveStatementFlags.IsNonLeafFrame) != 0;
    }
}
