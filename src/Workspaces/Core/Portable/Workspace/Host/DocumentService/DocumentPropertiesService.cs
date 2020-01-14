// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
