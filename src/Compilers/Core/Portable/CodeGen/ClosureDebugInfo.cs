// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct ClosureDebugInfo : IEquatable<ClosureDebugInfo>
    {
        public readonly int SyntaxOffset;
        public readonly DebugId ClosureId;

        public ClosureDebugInfo(int syntaxOffset, DebugId closureId)
        {
            SyntaxOffset = syntaxOffset;
            ClosureId = closureId;
        }

        public bool Equals(ClosureDebugInfo other)
        {
            return SyntaxOffset == other.SyntaxOffset &&
                   ClosureId.Equals(other.ClosureId);
        }

        public override bool Equals(object? obj)
        {
            return obj is ClosureDebugInfo && Equals((ClosureDebugInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(SyntaxOffset, ClosureId.GetHashCode());
        }

        internal string GetDebuggerDisplay()
        {
            return $"({ClosureId.GetDebuggerDisplay()} @{SyntaxOffset})";
        }
    }
}
