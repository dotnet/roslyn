// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class NullableFlowStateExtensions
    {
        public static bool MayBeNull(this NullableFlowState state) => state == NullableFlowState.MaybeNull;

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

#pragma warning disable IDE0055 // Fix formatting. This formatting is correct, need 16.1 for the updated formatter to not flag
        internal static CodeAnalysis.NullableFlowState ToPublicFlowState(this CSharp.NullableFlowState nullableFlowState) =>
            nullableFlowState switch
            {
                CSharp.NullableFlowState.NotNull => CodeAnalysis.NullableFlowState.NotNull,
                CSharp.NullableFlowState.MaybeNull => CodeAnalysis.NullableFlowState.MaybeNull,
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
#pragma warning restore IDE0055 // Fix formatting
    }
}
