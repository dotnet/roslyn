// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    [Flags]
    internal enum ExpansionFlags
    {
        None = 0x0,
        IncludeBaseMembers = 0x1,
        IncludeResultsView = 0x2,
        All = IncludeBaseMembers | IncludeResultsView,
    }

    /// <summary>
    /// The immediate children of a DkmEvaluationResult (e.g. the
    /// elements within an array). Ideally, the children are generated
    /// on demand, and not cached in the Expansion.
    /// </summary>
    internal abstract class Expansion
    {
        internal virtual bool ContainsFavorites => false;

        /// <summary>
        /// Get the rows within the given range. 'index' is advanced
        /// to the end of the range, or if 'visitAll' is true, 'index' is
        /// advanced to the end of the expansion.
        /// </summary>
        internal abstract void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResult> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index);

        internal static bool InRange(int startIndex, int count, int index)
        {
            return (index >= startIndex) && (index < startIndex + count);
        }

        internal static void GetIntersection(int startIndex1, int count1, int startIndex2, int count2, out int startIndex3, out int count3)
        {
            startIndex3 = Math.Max(startIndex1, startIndex2);
            int endIndex3 = Math.Min(startIndex1 + count1, startIndex2 + count2);
            count3 = Math.Max(endIndex3 - startIndex3, 0);
        }
    }
}
