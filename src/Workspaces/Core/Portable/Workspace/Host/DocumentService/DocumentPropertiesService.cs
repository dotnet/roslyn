// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Extensible document properties specified via a document service.
    /// </summary>
    internal class DocumentPropertiesService : IDocumentService
    {
        public static readonly DocumentPropertiesService Default = new DocumentPropertiesService();

        /// <summary>
        /// True if the source code contained in the document is only used in design-time (e.g. for completion),
        /// but is not passed to the compiler when the containing project is built.
        /// </summary>
        public virtual bool DesignTimeOnly => false;
    }
}
