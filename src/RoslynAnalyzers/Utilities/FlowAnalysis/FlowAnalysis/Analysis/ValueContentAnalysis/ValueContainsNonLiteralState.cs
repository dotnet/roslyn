// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    /// <summary>
    /// Value state for presence of non-literal values for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> tracked by <see cref="ValueContentAnalysis"/>.
    /// </summary>
    public enum ValueContainsNonLiteralState
    {
        /// <summary>The variable state is invalid due to predicate analysis.</summary>
        Invalid,
        /// <summary>State is undefined.</summary>
        Undefined,
        /// <summary>The variable does not contain any instances of a non-literal.</summary>
        No,
        /// <summary>The variable may or may not contain instances of a non-literal.</summary>
        Maybe,
    }
}
