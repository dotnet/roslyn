// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Specifies non-standard analyzer behaviors which impact testing.
    /// </summary>
    [Flags]
    public enum TestBehaviors
    {
        /// <summary>
        /// No special behaviors apply.
        /// </summary>
        None = 0,

        /// <summary>
        /// Skip the generated code exclusion check.
        /// </summary>
        /// <remarks>
        /// <para>By default, the analyzer test framework verifies that analyzer which report diagnostics do not report
        /// diagnostics in generated code. While some analyzers, e.g. security analyzers, are expected to report
        /// diagnostics in all code, most analyzers are expected to only report diagnostics in user-created code.</para>
        /// </remarks>
        SkipGeneratedCodeCheck = 0x01,

        /// <summary>
        /// Skip a verification check that diagnostics will not be reported if a <c>#pragma warning disable</c> appears
        /// at the beginning of the file.
        /// </summary>
        SkipSuppressionCheck = 0x02,
    }
}
