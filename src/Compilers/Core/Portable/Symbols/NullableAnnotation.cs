// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(nullable-api): Document
    public enum NullableAnnotation : byte
    {
        NotApplicable = 0,
        Disabled,      // No information. Think oblivious.
        NotAnnotated, // Type is not annotated - string, int, T (including the case when T is unconstrained).
        Annotated,    // Type is annotated - string?, T? where T : class; and for int?, T? where T : struct.
        NotNullable,  // Explicitly set by flow analysis
        Nullable,     // Explicitly set by flow analysis
    }
}
