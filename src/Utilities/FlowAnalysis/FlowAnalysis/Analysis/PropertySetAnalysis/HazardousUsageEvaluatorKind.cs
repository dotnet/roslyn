// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Distinguishes kinds of <see cref="HazardousUsageEvaluator"/>s.
    /// </summary>
    internal enum HazardousUsageEvaluatorKind
    {
        /// <summary>
        /// Evaluated at a method invocation.
        /// </summary>
        Invocation,

        /// <summary>
        /// Evaluated at a return value.
        /// </summary>
        Return,

        /// <summary>
        /// Evaluated at field and property initialization, or field or property assignments.
        /// </summary>
        Initialization,

        /// <summary>
        /// Evaluated at argument passing.
        /// </summary>
        Argument,
    }
}
