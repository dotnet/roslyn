// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Debug information maintained for each lambda.
    /// </summary>
    /// <remarks>
    /// The information is emitted to PDB in Custom Debug Information record for a method containing the lambda.
    /// </remarks>
    internal struct LambdaDebugInfo : IEquatable<LambdaDebugInfo>
    {
        /// <summary>
        /// The syntax offset of the syntax node declaring the lambda (lambda expression) or its body (lambda in a query).
        /// </summary>
        public readonly int SyntaxOffset;

        /// <summary>
        /// The ordinal of the closure frame the lambda belongs to, or -1 if not applicable 
        /// (static lambdas, lambdas closing over this pointer only).
        /// </summary>
        public readonly int ClosureOrdinal;

        public LambdaDebugInfo(int syntaxOffset, int closureOrdinal)
        {
            Debug.Assert(closureOrdinal >= -1);

            this.SyntaxOffset = syntaxOffset;
            this.ClosureOrdinal = closureOrdinal;
        }

        public bool Equals(LambdaDebugInfo other)
        {
            return this.SyntaxOffset == other.SyntaxOffset
                && this.ClosureOrdinal == other.ClosureOrdinal;
        }

        public override bool Equals(object obj)
        {
            return obj is LambdaDebugInfo && Equals((LambdaDebugInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ClosureOrdinal, SyntaxOffset);
        }

        public override string ToString()
        {
            return $"({SyntaxOffset}, {ClosureOrdinal})";
        }
    }
}
