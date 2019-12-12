// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the compiler's analysis of whether an expression may be null
    /// </summary>
    // Review docs: https://github.com/dotnet/roslyn/issues/35046
    public enum NullableFlowState : byte
    {
        /// <summary>
        /// Syntax is not an expression, or was not analyzed.
        /// </summary>
        None = 0,
        /// <summary>
        /// Expression is not null.
        /// </summary>
        NotNull,
        /// <summary>
        /// Expression may be null.
        /// </summary>
        MaybeNull
    }

    internal static class NullableFlowStateExtensions
    {
        public static NullableAnnotation ToAnnotation(this NullableFlowState nullableFlowState)
        {
            switch (nullableFlowState)
            {
                case CodeAnalysis.NullableFlowState.MaybeNull:
                    return CodeAnalysis.NullableAnnotation.Annotated;
                case CodeAnalysis.NullableFlowState.NotNull:
                    return CodeAnalysis.NullableAnnotation.NotAnnotated;
                default:
                    return CodeAnalysis.NullableAnnotation.None;
            }
        }
    }
}
