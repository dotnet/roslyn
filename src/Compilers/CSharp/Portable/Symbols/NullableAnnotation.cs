// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The nullable annotations that can apply in source.
    /// </summary>
    internal enum NullableAnnotation : byte
    {
        /// <summary>
        /// No information. Think oblivious.
        /// </summary>
        Unknown,

        /// <summary>
        /// Type is not annotated - string, int, T (including the case when T is unconstrained).
        /// </summary>
        NotAnnotated,

        /// <summary>
        /// Type is annotated - string?, T? where T : class; and for int?, T? where T : struct.
        /// </summary>
        Annotated,
    }
}
