// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Specifies characteristics of a file which cause diagnostics to not be reported.
    /// </summary>
    [Flags]
    public enum AnalysisExclusions
    {
        /// <summary>
        /// No special exclusions apply.
        /// </summary>
        None = 0,

        /// <summary>
        /// Analysis will not report diagnostics in generated code.
        /// </summary>
        GeneratedCode = 0x01,

        /// <summary>
        /// Diagnostics will not be reported if a <c>#pragma warning disable</c> appears at the beginning of the file.
        /// </summary>
        Suppression = 0x02,
    }
}
