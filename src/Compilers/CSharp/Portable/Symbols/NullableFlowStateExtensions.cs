// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class NullableFlowStateExtensions
    {
        public static bool MayBeNull(this NullableFlowState state) => state != NullableFlowState.NotNull;

        public static bool IsNotNull(this NullableFlowState state) => state == NullableFlowState.NotNull;

        /// <summary>
        /// Join nullable flow states from distinct branches during flow analysis.
        /// The result is <see cref="NullableFlowState.MaybeNull"/> if either operand is that.
        /// </summary>
        public static NullableFlowState Join(this NullableFlowState a, NullableFlowState b) => (a > b) ? a : b;

        /// <summary>
        /// Meet two nullable flow states from distinct states for the meet (union) operation in flow analysis.
        /// The result is <see cref="NullableFlowState.NotNull"/> if either operand is that.
        /// </summary>
        public static NullableFlowState Meet(this NullableFlowState a, NullableFlowState b) => (a < b) ? a : b;

        internal static CodeAnalysis.NullableFlowState ToPublicFlowState(this CSharp.NullableFlowState nullableFlowState) =>
            nullableFlowState switch
            {
                CSharp.NullableFlowState.NotNull => CodeAnalysis.NullableFlowState.NotNull,
                CSharp.NullableFlowState.MaybeNull => CodeAnalysis.NullableFlowState.MaybeNull,
                CSharp.NullableFlowState.MaybeDefault => CodeAnalysis.NullableFlowState.MaybeNull,
                _ => throw ExceptionUtilities.UnexpectedValue(nullableFlowState)
            };

        // https://github.com/dotnet/roslyn/issues/35035: remove if possible
        public static CSharp.NullableFlowState ToInternalFlowState(this CodeAnalysis.NullableFlowState flowState) =>
            flowState switch
            {
                CodeAnalysis.NullableFlowState.None => CSharp.NullableFlowState.NotNull,
                CodeAnalysis.NullableFlowState.NotNull => CSharp.NullableFlowState.NotNull,
                CodeAnalysis.NullableFlowState.MaybeNull => CSharp.NullableFlowState.MaybeNull,
                _ => throw ExceptionUtilities.UnexpectedValue(flowState)
            };
    }
}
