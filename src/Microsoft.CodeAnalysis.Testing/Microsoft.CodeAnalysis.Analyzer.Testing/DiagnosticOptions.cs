// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
