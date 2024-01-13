// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Specifies a kind of instrumentation to be applied in generated code.
    /// </summary>
    public enum InstrumentationKind
    {
        /// <summary>
        /// No instrumentation.
        /// </summary>
        None = 0,

        /// <summary>
        /// Instruments the code to add test coverage.
        /// </summary>
        TestCoverage = 1,
    }

    internal static class InstrumentationKindExtensions
    {
        internal const InstrumentationKind LocalStateTracing = (InstrumentationKind)(-1);

        internal static bool IsValid(this InstrumentationKind value)
        {
            return value >= InstrumentationKind.None && value <= InstrumentationKind.TestCoverage;
        }
    }
}
