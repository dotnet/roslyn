// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct LambdaDebugInfo : IEquatable<LambdaDebugInfo>
    {
        /// <summary>
        /// The syntax offset of the syntax node declaring the lambda (lambda expression) or its body (lambda in a query).
        /// </summary>
        public readonly int SyntaxOffset;

        /// <summary>
        /// The ordinal of the closure frame the lambda belongs to, or
        /// <see cref="StaticClosureOrdinal"/> if the lambda is static, or
        /// <see cref="ThisOnlyClosureOrdinal"/> if the lambda is closed over "this" pointer only.
        /// </summary>
        public readonly int ClosureOrdinal;

        public readonly DebugId LambdaId;

        public const int StaticClosureOrdinal = -1;
        public const int ThisOnlyClosureOrdinal = -2;
        public const int MinClosureOrdinal = ThisOnlyClosureOrdinal;

        public LambdaDebugInfo(int syntaxOffset, DebugId lambdaId, int closureOrdinal)
        {
            Debug.Assert(closureOrdinal >= MinClosureOrdinal);

            SyntaxOffset = syntaxOffset;
            ClosureOrdinal = closureOrdinal;
            LambdaId = lambdaId;
        }

        public bool Equals(LambdaDebugInfo other)
        {
            return SyntaxOffset == other.SyntaxOffset
                && ClosureOrdinal == other.ClosureOrdinal
                && LambdaId.Equals(other.LambdaId);
        }

        public override bool Equals(object? obj)
        {
            return obj is LambdaDebugInfo && Equals((LambdaDebugInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ClosureOrdinal,
                   Hash.Combine(SyntaxOffset, LambdaId.GetHashCode()));
        }

        internal string GetDebuggerDisplay()
        {
            return
                ClosureOrdinal == StaticClosureOrdinal ? $"({LambdaId.GetDebuggerDisplay()} @{SyntaxOffset}, static)" :
                ClosureOrdinal == ThisOnlyClosureOrdinal ? $"(#{LambdaId.GetDebuggerDisplay()} @{SyntaxOffset}, this)" :
                $"({LambdaId.GetDebuggerDisplay()} @{SyntaxOffset} in {ClosureOrdinal})";
        }
    }
}
