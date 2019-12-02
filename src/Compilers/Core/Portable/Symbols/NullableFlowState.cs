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
}
