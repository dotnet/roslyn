// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
