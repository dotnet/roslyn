// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Emit
{
    public enum InstrumentationKind
    {
        None = 0,
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
