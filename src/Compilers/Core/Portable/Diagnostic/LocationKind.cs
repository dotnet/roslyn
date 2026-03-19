// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the kind of location (source vs. metadata).
    /// </summary>
    public enum LocationKind : byte
    {
        /// <summary>
        /// Unspecified location.
        /// </summary>
        None = 0,

        /// <summary>
        /// The location represents a position in a source file.
        /// </summary>
        SourceFile = 1,

        /// <summary>
        /// The location represents a metadata file.
        /// </summary>
        MetadataFile = 2,

        /// <summary>
        /// The location represents a position in an XML file.
        /// </summary>
        XmlFile = 3,

        /// <summary>
        /// The location in some external file.
        /// </summary>
        ExternalFile = 4,
    }
}
