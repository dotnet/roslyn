// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal struct ClosureDebugInfo : IEquatable<ClosureDebugInfo>
    {
        public readonly int SyntaxOffset;

        public ClosureDebugInfo(int syntaxOffset)
        {
            this.SyntaxOffset = syntaxOffset;
        }

        public bool Equals(ClosureDebugInfo other)
        {
            return this.SyntaxOffset == other.SyntaxOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is ClosureDebugInfo && Equals((ClosureDebugInfo)obj);
        }

        public override int GetHashCode()
        {
            return SyntaxOffset.GetHashCode();
        }

        public override string ToString()
        {
            return $"({SyntaxOffset})";
        }
    }
}
