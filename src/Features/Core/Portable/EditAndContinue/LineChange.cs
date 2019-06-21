// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct LineChange : IEquatable<LineChange>
    {
        public readonly int OldLine;
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
        {
            return obj is LineChange && Equals((LineChange)obj);
        }

        public bool Equals(LineChange other)
        {
            return OldLine == other.OldLine
                && NewLine == other.NewLine;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(OldLine, NewLine);
        }

        public override string ToString()
        {
            return OldLine.ToString() + " -> " + NewLine.ToString();
        }
    }
}
