// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Instruments the binary to add test coverage.
        /// </summary>
        TestCoverage = 1,
    }

    internal static class InstrumentationKindExtensions
    {
        internal static bool IsValid(this InstrumentationKind value)
        {
            return value >= InstrumentationKind.None && value <= InstrumentationKind.TestCoverage;
        }
    }
}
