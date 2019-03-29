// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

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
        public static NullableAnnotation Join(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a.IsAnnotated() || b.IsAnnotated())
                return NullableAnnotation.Annotated;
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Meet two nullable annotations for computing the nullable annotation of a type parameter from upper bounds.
        /// This uses the contravariant merging rules.
        /// </summary>
        public static NullableAnnotation Meet(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a.IsNotAnnotated() || b.IsNotAnnotated())
                return NullableAnnotation.NotAnnotated;
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Check that two nullable annotations are "compatible", which means they could be the same. Return the
        /// nullable annotation to be used as a result.  This uses the invariant merging rules.
        /// </summary>
        public static NullableAnnotation EnsureCompatible(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a.IsOblivious())
                return b;
            if (b.IsOblivious())
                return a;
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Merges nullability.
        /// </summary>
        public static NullableAnnotation MergeNullableAnnotation(this NullableAnnotation a, NullableAnnotation b, VarianceKind variance)
        {
            return variance switch
            {
                VarianceKind.In => a.Meet(b),
                VarianceKind.Out => a.Join(b),
                VarianceKind.None => a.EnsureCompatible(b),
                _ => throw ExceptionUtilities.UnexpectedValue(variance)
            };
        }
    }
}
