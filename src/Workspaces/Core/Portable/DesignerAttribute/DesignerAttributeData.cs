// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    /// <summary>
    /// Serialization typed used to pass information to/from OOP and VS.
    /// </summary>
    internal struct DesignerAttributeData
    {
        /// <summary>
        /// The category specified in a <c>[DesignerCategory("...")]</c> attribute.
        /// </summary>
        public string? Category;

        /// <summary>
        /// The document this <see cref="Category"/> applies to.
        /// </summary>
        public DocumentId DocumentId;

        /// <summary>
        /// Path for this <see cref="DocumentId"/>.
        /// </summary>
        public string FilePath;
    }
}
