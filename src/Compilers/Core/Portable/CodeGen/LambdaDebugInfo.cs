// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Debug information maintained for each lambda.
    /// </summary>
    /// <remarks>
    /// The information is emitted to PDB in Custom Debug Information record for a method containing the lambda.
    /// </remarks>
    internal readonly record struct LambdaDebugInfo
    {
        /// <summary>
        /// The syntax offset of the syntax node declaring the lambda (lambda expression) or its body (lambda in a query).
        /// </summary>
        public readonly int SyntaxOffset;

        /// <summary>
        /// The ordinal of the closure frame the lambda or local function belongs to, or
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

        public override string ToString()
            => ClosureOrdinal == StaticClosureOrdinal ? $"({LambdaId} @{SyntaxOffset}, static)" :
               ClosureOrdinal == ThisOnlyClosureOrdinal ? $"(#{LambdaId} @{SyntaxOffset}, this)" :
               $"({LambdaId} @{SyntaxOffset} in {ClosureOrdinal})";
    }
}
