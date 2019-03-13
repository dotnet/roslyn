// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class NullableAnnotationExtensions
    {
        public static bool IsAnyNullable(this NullableAnnotation annotation)
        {
            return annotation == NullableAnnotation.Annotated || annotation == NullableAnnotation.Nullable;
        }

        public static bool IsAnyNotNullable(this NullableAnnotation annotation)
        {
            return annotation == NullableAnnotation.NotAnnotated || annotation == NullableAnnotation.NotNullable;
        }

        public static bool IsSpeakable(this NullableAnnotation annotation)
        {
            return annotation == NullableAnnotation.Unknown ||
                annotation == NullableAnnotation.NotAnnotated ||
                annotation == NullableAnnotation.Annotated;
        }

        /// <summary>
        /// This method projects nullable annotations onto a smaller set that can be expressed in source.
        /// </summary>
        public static NullableAnnotation AsSpeakable(this NullableAnnotation annotation, TypeSymbol type)
        {
            if (type is null && annotation == NullableAnnotation.Unknown)
            {
                return default;
            }

            Debug.Assert((object)type != null);
            switch (annotation)
            {
                case NullableAnnotation.Unknown:
                case NullableAnnotation.NotAnnotated:
                case NullableAnnotation.Annotated:
                    return annotation;

                case NullableAnnotation.Nullable:
                    if (type.IsTypeParameterDisallowingAnnotation())
                    {
                        return NullableAnnotation.NotAnnotated;
                    }
                    return NullableAnnotation.Annotated;

                case NullableAnnotation.NotNullable:
                    // Example of unspeakable types:
                    // - an unconstrained T which was null-tested already
                    // - a nullable value type which was null-tested already
                    // Note this projection is lossy for such types (we forget about the non-nullable state)
                    return NullableAnnotation.NotAnnotated;

                default:
                    throw ExceptionUtilities.UnexpectedValue(annotation);
            }
        }

        /// <summary>
        /// Join nullable annotations from the set of lower bounds for fixing a type parameter.
        /// This uses the covariant merging rules.
        /// </summary>
        public static NullableAnnotation JoinForFixingLowerBounds(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a == NullableAnnotation.Nullable || b == NullableAnnotation.Nullable)
            {
                return NullableAnnotation.Nullable;
            }

            if (a == NullableAnnotation.Annotated || b == NullableAnnotation.Annotated)
            {
                return NullableAnnotation.Annotated;
            }

            if (a == NullableAnnotation.Unknown || b == NullableAnnotation.Unknown)
            {
                return NullableAnnotation.Unknown;
            }

            if (a == NullableAnnotation.NotNullable || b == NullableAnnotation.NotNullable)
            {
                return NullableAnnotation.NotNullable;
            }

            return NullableAnnotation.NotAnnotated;
        }

        /// <summary>
        /// Join nullable flow states from distinct branches during flow analysis.
        /// </summary>
        public static NullableFlowState JoinForFlowAnalysisBranches(this NullableFlowState selfState, NullableFlowState otherState)
        {
            return (selfState == NullableFlowState.MaybeNull || otherState == NullableFlowState.MaybeNull)
                ? NullableFlowState.MaybeNull : NullableFlowState.NotNull;
        }

        /// <summary>
        /// Meet two nullable annotations for computing the nullable annotation of a type parameter from upper bounds.
        /// This uses the contravariant merging rules.
        /// </summary>
        public static NullableAnnotation MeetForFixingUpperBounds(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a == NullableAnnotation.NotNullable || b == NullableAnnotation.NotNullable)
            {
                return NullableAnnotation.NotNullable;
            }

            if (a == NullableAnnotation.NotAnnotated || b == NullableAnnotation.NotAnnotated)
            {
                return NullableAnnotation.NotAnnotated;
            }

            if (a == NullableAnnotation.Unknown || b == NullableAnnotation.Unknown)
            {
                return NullableAnnotation.Unknown;
            }

            if (a == NullableAnnotation.Nullable || b == NullableAnnotation.Nullable)
            {
                return NullableAnnotation.Nullable;
            }

            return NullableAnnotation.Annotated;
        }

        /// <summary>
        /// Meet two nullable flow states from distinct states for the meet (union) operation in flow analysis.
        /// </summary>
        public static NullableFlowState MeetForFlowAnalysisFinally(this NullableFlowState selfState, NullableFlowState otherState)
        {
            return (selfState == NullableFlowState.NotNull || otherState == NullableFlowState.NotNull)
                ? NullableFlowState.NotNull : NullableFlowState.MaybeNull;
        }

        /// <summary>
        /// Check that two nullable annotations are "compatible", which means they could be the same. Return the
        /// nullable annotation to be used as a result.
        /// This uses the invariant merging rules.
        /// </summary>
        public static NullableAnnotation EnsureCompatible(this NullableAnnotation a, NullableAnnotation b)
        {
            Debug.Assert(a.IsSpeakable());
            Debug.Assert(b.IsSpeakable());

            if (a == NullableAnnotation.NotAnnotated || b == NullableAnnotation.NotAnnotated)
            {
                return NullableAnnotation.NotAnnotated;
            }

            if (a == NullableAnnotation.Annotated || b == NullableAnnotation.Annotated)
            {
                return NullableAnnotation.Annotated;
            }

            return NullableAnnotation.Unknown;
        }

        /// <summary>
        /// Check that two nullable annotations are "compatible", which means they could be the same. Return the
        /// nullable annotation to be used as a result. This method can handle unspeakable types (for merging tuple types).
        /// </summary>
        public static NullableAnnotation EnsureCompatibleForTuples(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a == NullableAnnotation.NotNullable || b == NullableAnnotation.NotNullable)
            {
                return NullableAnnotation.NotNullable;
            }

            if (a == NullableAnnotation.NotAnnotated || b == NullableAnnotation.NotAnnotated)
            {
                return NullableAnnotation.NotAnnotated;
            }

            if (a == NullableAnnotation.Nullable || b == NullableAnnotation.Nullable)
            {
                return NullableAnnotation.Nullable;
            }

            if (a == NullableAnnotation.Annotated || b == NullableAnnotation.Annotated)
            {
                return NullableAnnotation.Annotated;
            }

            return NullableAnnotation.Unknown;
        }

        internal static CodeAnalysis.NullableAnnotation ToPublicAnnotation(this NullableAnnotation annotation)
        {
            Debug.Assert((CodeAnalysis.NullableAnnotation)(NullableAnnotation.Unknown + 1) == CodeAnalysis.NullableAnnotation.Unknown);
            return (CodeAnalysis.NullableAnnotation)annotation + 1;
        }
    }
}
