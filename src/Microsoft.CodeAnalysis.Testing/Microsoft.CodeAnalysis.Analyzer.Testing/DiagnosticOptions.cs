// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Defines options for interpreting <see cref="DiagnosticResult"/>.
    /// </summary>
    [Flags]
    public enum DiagnosticOptions
    {
        /// <summary>
        /// The result should be interpreted using the default settings.
        /// </summary>
        None = 0,

        /// <summary>
        /// The primary diagnostic location is defined, but additional locations have not been provided. Disables
        /// validation of additional locations reported for the corresponding diagnostics.
        /// </summary>
        IgnoreAdditionalLocations = 1,

        /// <summary>
        /// Ignore the diagnostic severity when verifying this diagnostic result.
        /// </summary>
        IgnoreSeverity = 2,
    }
}
