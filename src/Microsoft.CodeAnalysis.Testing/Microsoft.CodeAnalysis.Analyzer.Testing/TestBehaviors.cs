// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
