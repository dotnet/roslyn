// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Represents a sequence point before translation by #line/ExternalSource directives.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct RawSequencePoint
    {
        internal readonly SyntaxTree SyntaxTree;
        internal readonly int ILMarker;
        internal readonly TextSpan Span;

        // Special text span indicating a hidden sequence point.
        internal static readonly TextSpan HiddenSequencePointSpan = new TextSpan(0x7FFFFFFF, 0);

        internal RawSequencePoint(SyntaxTree syntaxTree, int ilMarker, TextSpan span)
        {
            this.SyntaxTree = syntaxTree;
            this.ILMarker = ilMarker;
            this.Span = span;
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("#{0}: {1}", ILMarker, Span == HiddenSequencePointSpan ? "hidden" : Span.ToString());
        }
    }
}
