// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the nullability of values that can be assigned
    /// to an expression used as an lvalue.
    /// </summary>
    // Review docs: https://github.com/dotnet/roslyn/issues/35046
    public enum NullableAnnotation : byte
    {
        /// <summary>
        /// The expression has not been analyzed, or the syntax is
        /// not an expression (such as a statement).
        /// </summary>
        None = 0,
        /// <summary>
        /// The expression is not annotated (does not have a ?).
        /// </summary>
        NotAnnotated,
        /// <summary>
        /// The expression is annotated (does have a ?).
        /// </summary>
        Annotated,
    }
}
