// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SyntaxDiagnosticInfo : DiagnosticInfo
    {
        /// <summary>
        /// The offset of this diagnostic, relative to the <em>Start</em> of the <see cref="GreenNode"/> (not the
        /// FullStart) it is attached to.  (Note: 'Start'/'FullStart' are properties of red Syntax elements, not
        /// GreenNodes. However, the above holds for the red elements created for the green node in its particular
        /// context.
        /// <para/>
        /// It is legal for the offset to be negative.  Or for the final calculated position to extend beyond the
        /// FullSpan or Span of the entity that it is on.  For example, a diagnostic may be placed on a node
        /// corresponding to a token seen before/after that node.  Diagnostics are often attached to what is convenient
        /// in the parser, not necessarily the exact syntactic construct they may be reporting their span under.
        /// </summary>
        internal readonly int Offset;

        /// <summary>
        /// Represents the width of the diagnostic.  Must be non-negative, but may be zero.
        /// </summary>
        internal readonly int Width;

        internal SyntaxDiagnosticInfo(int offset, int width, ErrorCode code, params object[] args)
            : base(CSharp.MessageProvider.Instance, (int)code, args)
        {
            Debug.Assert(width >= 0);
            this.Offset = offset;
            this.Width = width;
        }

        internal SyntaxDiagnosticInfo(int offset, int width, ErrorCode code)
            : this(offset, width, code, Array.Empty<object>())
        {
        }

        internal SyntaxDiagnosticInfo(ErrorCode code, params object[] args)
            : this(0, 0, code, args)
        {
        }

        internal SyntaxDiagnosticInfo(ErrorCode code)
            : this(0, 0, code)
        {
        }

        public SyntaxDiagnosticInfo WithOffset(int offset)
        {
            return new SyntaxDiagnosticInfo(offset, this.Width, (ErrorCode)this.Code, this.Arguments);
        }

        protected SyntaxDiagnosticInfo(SyntaxDiagnosticInfo original, DiagnosticSeverity severity) : base(original, severity)
        {
            Offset = original.Offset;
            Width = original.Width;
        }

        protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
        {
            return new SyntaxDiagnosticInfo(this, severity);
        }
    }
}
