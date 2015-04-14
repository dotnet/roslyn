// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal struct ClosureDebugInfo : IEquatable<ClosureDebugInfo>
    {
        public readonly int SyntaxOffset;
        public readonly int Generation;

        public ClosureDebugInfo(int syntaxOffset, int generation)
        {
            Debug.Assert(generation >= 0);

            SyntaxOffset = syntaxOffset;
            Generation = generation;
        }

        public bool Equals(ClosureDebugInfo other)
        {
            return SyntaxOffset == other.SyntaxOffset &&
                   Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is ClosureDebugInfo && Equals((ClosureDebugInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(SyntaxOffset, Generation);
        }

        public override string ToString()
        {
            return $"(#{Generation} @{SyntaxOffset})";
        }
    }
}
