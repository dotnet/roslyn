// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.StringContentAnalysis
{
    /// <summary>
    /// String state for presence of non-literal values for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> tracked by <see cref="StringContentAnalysis"/>.
    /// </summary>
    internal enum StringContainsNonLiteralState
    {
        /// <summary>The variable state is invalid due to predicate analysis.</summary>
        Invalid,
        /// <summary>State is undefined.</summary>
        Undefined,
        /// <summary>The variable does not contain any instances of non-literal string.</summary>
        No,
        /// <summary>The variable may or may not contain instances of a non-literal string.</summary>
        Maybe,
    }
}
