// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    /// <summary>
    /// An enum to identify what kind of completion a given Completion
    /// represents.
    /// </summary>

    public enum EditorConfigCompletionKind
    {
        /// <summary>
        /// The completion represents an attribute property.
        /// </summary>
        Property,

        /// <summary>
        /// The completion represents an attribute value.
        /// </summary>
        Value,
    }
}
