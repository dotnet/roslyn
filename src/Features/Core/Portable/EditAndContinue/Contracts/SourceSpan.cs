// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct SourceSpan : IEquatable<SourceSpan>
    {
        [DataMember(Order = 0)]
        public readonly int StartLine;

        [DataMember(Order = 1)]
        public readonly int StartColumn;

        [DataMember(Order = 2)]
        public readonly int EndLine;

        [DataMember(Order = 3)]
        public readonly int EndColumn;

        public SourceSpan(int startLine, int startColumn, int endLine, int endColumn)
        {
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }

        public override bool Equals(object? obj)
            => obj is SourceSpan span && Equals(span);

        public bool Equals(SourceSpan other)
            => StartLine == other.StartLine &&
               EndLine == other.EndLine &&
               StartColumn == other.StartColumn &&
               EndColumn == other.EndColumn;

        public override int GetHashCode()
            => Hash.Combine(StartLine, Hash.Combine(EndLine, Hash.Combine(StartColumn, EndColumn)));

        public static bool operator ==(SourceSpan left, SourceSpan right) => left.Equals(right);
        public static bool operator !=(SourceSpan left, SourceSpan right) => !(left == right);

        internal string GetDebuggerDisplay()
            => $"({StartLine},{StartColumn})-({EndLine},{EndColumn})";
    }
}
