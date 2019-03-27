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

        internal static CodeAnalysis.NullableFlowState ToPublicFlowState(this NullableFlowState nullableFlowState) => nullableFlowState switch
        {
            NullableFlowState.NotNull => CodeAnalysis.NullableFlowState.NotNull,
            NullableFlowState.MaybeNull => CodeAnalysis.NullableFlowState.MaybeNull,
            _ => throw ExceptionUtilities.UnexpectedValue(nullableFlowState)
        };

        // PROTOTYPE(nullable-api): remove if possible
        public static NullableFlowState ToInternalFlowState(this CodeAnalysis.NullableFlowState flowState) => flowState switch
        {
            CodeAnalysis.NullableFlowState.NotApplicable => NullableFlowState.NotNull,
            CodeAnalysis.NullableFlowState.NotNull => NullableFlowState.NotNull,
            CodeAnalysis.NullableFlowState.MaybeNull => NullableFlowState.MaybeNull,
            _ => throw ExceptionUtilities.UnexpectedValue(flowState)
        };
    }
}
