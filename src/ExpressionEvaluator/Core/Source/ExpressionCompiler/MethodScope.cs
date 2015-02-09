// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class MethodScope : IEquatable<MethodScope>
    {
        internal readonly int MethodToken;
        internal readonly int MethodVersion;
        internal readonly int StartOffset;
        internal readonly int EndOffset;

        internal MethodScope(int methodToken, int methodVersion, int startOffset, int endOffset)
        {
            Debug.Assert(MetadataTokens.Handle(methodToken).Kind == HandleKind.MethodDefinition);
            Debug.Assert((startOffset >= 0) == (endOffset >= 0));

            this.MethodToken = methodToken;
            this.MethodVersion = methodVersion;
            this.StartOffset = startOffset;
            this.EndOffset = endOffset;
        }

        public bool Equals(MethodScope other)
        {
            if (other == null)
            {
                return false;
            }
            return (this.MethodToken == other.MethodToken) &&
                (this.MethodVersion == other.MethodVersion) &&
                (this.StartOffset == other.StartOffset) &&
                (this.EndOffset == other.EndOffset);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MethodScope);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.MethodToken, Hash.Combine(this.MethodVersion, Hash.Combine(this.StartOffset, this.EndOffset)));
        }
    }
}
