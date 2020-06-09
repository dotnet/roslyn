// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct LineChange : IEquatable<LineChange>
    {
        /// <summary>
        /// Zero-based line number.
        /// </summary>
        public readonly int OldLine;

        /// <summary>
        /// Zero-based line number.
        /// </summary>
        public readonly int NewLine;

        internal LineChange(int oldLine, int newLine)
        {
            Debug.Assert(oldLine >= 0);
            Debug.Assert(newLine >= 0);
            Debug.Assert(oldLine != newLine);

            OldLine = oldLine;
            NewLine = newLine;
        }

        public override bool Equals(object obj)
            => obj is LineChange && Equals((LineChange)obj);

        public bool Equals(LineChange other)
            => OldLine == other.OldLine && NewLine == other.NewLine;

        public override int GetHashCode()
            => Hash.Combine(OldLine, NewLine);

        public override string ToString()
            => $"{OldLine} -> {NewLine}";
    }
}
