// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The nullable annotations that can apply in source.
    /// </summary>
    /// <remarks>
    /// The order of values here is used in the computation of <see cref="NullableAnnotationExtensions.Meet(NullableAnnotation, NullableAnnotation)"/>,
    /// <see cref="NullableAnnotationExtensions.Join(NullableAnnotation, NullableAnnotation)"/>, and
    /// <see cref="NullableAnnotationExtensions.EnsureCompatible(NullableAnnotation, NullableAnnotation)"/>.  If the order here is changed
    /// then those implementations may have to be revised (or simplified).
    /// </remarks>
    internal enum NullableAnnotation : byte
    {
        /// <summary>
        /// Type is not annotated - string, int, T (including the case when T is unconstrained).
        /// </summary>
        NotAnnotated,

        /// <summary>
        /// The type is not annotated in a context where the nullable feature is not enabled.
        /// Used for interoperation with existing pre-nullable code.
        /// </summary>
        Oblivious,

        /// <summary>
        /// Type is annotated with '?' - string?, T? where T : class; and for int?, T? where T : struct.
        /// </summary>
        /// <remarks>
        /// A type must be known to be a (non-nullable)
        /// type in order to be <see cref="Annotated"/>.  Therefore type parameters typically cannot be <see cref="Annotated"/> --
        /// only a type parameter that is constrained to a non-nullable type can be <see cref="Annotated"/>.
        /// </remarks>
        Annotated,
    }
}
