// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Specifies the type of an instrumentation that is performed and be emitted along with the binary output.
    /// </summary>
    public enum InstrumentationKind
    {
        /// <summary>
        /// No instrumentation.
        /// </summary>
        None = 0,

        /// <summary>
        /// Instruments the binary to add....
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
