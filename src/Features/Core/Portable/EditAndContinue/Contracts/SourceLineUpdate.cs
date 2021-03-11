// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    internal readonly struct SourceLineUpdate
        : IEquatable<SourceLineUpdate>
    {
        /// <summary>
        /// Zero-based line number.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly int OldLine;

        /// <summary>
        /// Zero-based line number.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly int NewLine;

        internal SourceLineUpdate(int oldLine, int newLine)
        {
            Debug.Assert(oldLine >= 0);
            Debug.Assert(newLine >= 0);
            Debug.Assert(oldLine != newLine);

            OldLine = oldLine;
            NewLine = newLine;
        }

        public override bool Equals(object? obj)
            => obj is SourceLineUpdate change && Equals(change);

        public bool Equals(SourceLineUpdate other)
            => OldLine == other.OldLine && NewLine == other.NewLine;

        public override int GetHashCode()
            => Hash.Combine(OldLine, NewLine);

        public override string ToString()
            => $"{OldLine} -> {NewLine}";
    }
}
