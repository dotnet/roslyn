// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Abstract analysis domain for a <see cref="DataFlowAnalysis"/> to merge and compare analysis data.
    /// </summary>
    public abstract class AbstractAnalysisDomain<TAnalysisData> where TAnalysisData : AbstractAnalysisData
    {
        /// <summary>
        /// Creates a clone of the analysis data.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract TAnalysisData Clone(TAnalysisData value);

        /// <summary>
        /// Returns a value that is greater than <paramref name="value1"/> and <paramref name="value2"/>.
        /// </summary>
        /// <param name="value1">A value to be merged</param>
        /// <param name="value2">A value to be merged</param>
        /// <returns>A value that is greater than <paramref name="value1"/> and <paramref name="value2"/></returns>
        public abstract TAnalysisData Merge(TAnalysisData value1, TAnalysisData value2);

        /// <summary>
        /// Compares <paramref name="oldValue"/> with <paramref name="newValue"/>
        /// and returns a value indicating whether one value is less than,
        /// equal to, or greater than the other.
        /// </summary>
        /// <param name="oldValue">A value to compare</param>
        /// <param name="newValue">A value to compare</param>
        /// <returns>A signed integer that indicates the relative values of
        /// <paramref name="oldValue"/> and <paramref name="newValue"/>.
        /// <para>Less than zero: <paramref name="oldValue"/> is less than <paramref name="newValue"/>.</para>
        /// <para>Zero: <paramref name="oldValue"/> equals <paramref name="newValue"/>.</para>
        /// <para>Greater than zero: <paramref name="oldValue"/> is greater than <paramref name="newValue"/>.</para>
        ///</returns>
        public abstract int Compare(TAnalysisData oldValue, TAnalysisData newValue);

        /// <summary>
        /// Indicates if <paramref name="value1"/> with <paramref name="value2"/> are equal.
        /// </summary>
        /// <param name="value1">A value to compare</param>
        /// <param name="value2">A value to compare</param>
        public abstract bool Equals(TAnalysisData value1, TAnalysisData value2);
    }
}
