// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Represents an expected <see cref="Location"/> appearing in <see cref="Diagnostic.Location"/> or
    /// <see cref="Diagnostic.AdditionalLocations"/>.
    /// </summary>
    public readonly struct DiagnosticLocation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticLocation"/> structure with the specified location and
        /// options.
        /// </summary>
        /// <param name="span">The location of the diagnostic.</param>
        /// <param name="options">The options to consider when validating this location.</param>
        public DiagnosticLocation(FileLinePositionSpan span, DiagnosticLocationOptions options)
        {
            Span = span;
            Options = options;
        }

        /// <summary>
        /// Gets the file and span of the location.
        /// </summary>
        public FileLinePositionSpan Span { get; }

        /// <summary>
        /// Gets the options for validating the location.
        /// </summary>
        public DiagnosticLocationOptions Options { get; }
    }
}
