// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Kind for the <see cref="CopyAbstractValue"/>.
    /// </summary>
    public enum CopyAbstractValueKind
    {
        /// <summary>
        /// Not applicable.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// Copy of a reference shared by one or more <see cref="AnalysisEntity"/> instances.
        /// </summary>
        KnownReferenceCopy,

        /// <summary>
        /// Copy of a value shared by one or more <see cref="AnalysisEntity"/> instances.
        /// </summary>
        KnownValueCopy,

        /// <summary>
        /// Copy may or may not be shared by other <see cref="AnalysisEntity"/> instances.
        /// </summary>
        Unknown,

        /// <summary>
        /// Invalid state for an unreachable path from predicate analysis.
        /// </summary>
        Invalid,
    }

    internal static class CopyAbstractValueKindExtensions
    {
        public static bool IsKnown(this CopyAbstractValueKind kind)
        {
            return kind switch
            {
                CopyAbstractValueKind.KnownValueCopy
                or CopyAbstractValueKind.KnownReferenceCopy => true,
                _ => false,
            };
        }

        public static CopyAbstractValueKind MergeIfBothKnown(this CopyAbstractValueKind kind, CopyAbstractValueKind kindToMerge)
        {
            if (!kind.IsKnown() ||
                !kindToMerge.IsKnown())
            {
                return kind;
            }

            // Can only ensure value copy if one of the kinds is a value copy.
            return kind == CopyAbstractValueKind.KnownValueCopy || kindToMerge == CopyAbstractValueKind.KnownValueCopy ?
                CopyAbstractValueKind.KnownValueCopy :
                CopyAbstractValueKind.KnownReferenceCopy;
        }
    }
}
