// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

// https://github.com/dotnet/roslyn/issues/34962 IDE005 "Fix formatting" does a poor job with a switch expression as the body of an expression-bodied method
#pragma warning disable IDE0055

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class NullableAnnotationExtensions
    {
        public static bool IsAnnotated(this NullableAnnotation annotation) => annotation == NullableAnnotation.Annotated;

        public static bool IsNotAnnotated(this NullableAnnotation annotation) => annotation == NullableAnnotation.NotAnnotated;

        public static bool IsOblivious(this NullableAnnotation annotation) => annotation == NullableAnnotation.Oblivious;

        /// <summary>
        /// Join nullable annotations from the set of lower bounds for fixing a type parameter.
        /// This uses the covariant merging rules.
        /// </summary>
        public static NullableAnnotation Join(this NullableAnnotation a, NullableAnnotation b) => (a < b) ? b : a;

        /// <summary>
        /// Meet two nullable annotations for computing the nullable annotation of a type parameter from upper bounds.
        /// This uses the contravariant merging rules.
        /// </summary>
        public static NullableAnnotation Meet(this NullableAnnotation a, NullableAnnotation b) => (a < b) ? a : b;

        /// <summary>
        /// Return the nullable annotation to use when two annotations are expected to be "compatible", which means
        /// they could be the same. These are the "invariant" merging rules.
        /// </summary>
        public static NullableAnnotation EnsureCompatible(this NullableAnnotation a, NullableAnnotation b) =>
            (a, b) switch
            {
                (NullableAnnotation.Oblivious, _) => b,
                (_, NullableAnnotation.Oblivious) => a,
                _ => a < b ? a : b,
            };

        /// <summary>
        /// Merges nullability.
        /// </summary>
        public static NullableAnnotation MergeNullableAnnotation(this NullableAnnotation a, NullableAnnotation b, VarianceKind variance) =>
            variance switch
            {
                VarianceKind.In => a.Meet(b),
                VarianceKind.Out => a.Join(b),
                VarianceKind.None => a.EnsureCompatible(b),
                _ => throw ExceptionUtilities.UnexpectedValue(variance)
            };

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.NotAnnotated"/>.
        /// </summary>
        public const byte NotAnnotatedAttributeValue = 1;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.Annotated"/>.
        /// </summary>
        public const byte AnnotatedAttributeValue = 2;

        /// <summary>
        /// The attribute (metadata) representation of <see cref="NullableAnnotation.Oblivious"/>.
        /// </summary>
        public const byte ObliviousAttributeValue = 0;
    }
}
