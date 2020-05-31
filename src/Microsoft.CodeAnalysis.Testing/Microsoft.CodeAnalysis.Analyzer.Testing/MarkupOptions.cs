// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Specifies additional options for the markup parser.
    /// </summary>
    [Flags]
    public enum MarkupOptions
    {
        /// <summary>
        /// No additional markup options are specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Use the first matching diagnostic descriptor when multiple diagnostics match the syntax. By default, this
        /// option is not specified and the markup parser will fail when the syntax does not represent a <em>unique</em>
        /// descriptor.
        /// </summary>
        UseFirstDescriptor = 0x0001,
    }
}
