// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the compiler's analysis of whether an expression can be null.
    /// </summary>
    // Review docs: https://github.com/dotnet/roslyn/issues/35046
    public enum NullableFlowState : byte
    {
        /// <summary>
        /// Syntax is not an expression, or was not analyzed.
        /// </summary>
        NotApplicable = 0,
        /// <summary>
        /// Expression cannot contain null.
        /// </summary>
        NotNull,
        /// <summary>
        /// Expression can contain null.
        /// </summary>
        MaybeNull
    }
}
