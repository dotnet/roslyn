// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Represents a shadow copy of an assembly or a standalone module.
    /// </summary>
    public sealed class MetadataShadowCopy
    {
        /// <summary>
        /// Assembly manifest module copy or a standalone module copy.
        /// </summary>
        public ShadowCopy PrimaryModule { get; }

        /// <summary>
        /// Documentation file copy or null if there is none.
        /// </summary>
        public ShadowCopy DocumentationFile { get; }

        // this instance doesn't own the image
        public Metadata Metadata { get; }

        internal MetadataShadowCopy(ShadowCopy primaryModule, ShadowCopy documentationFile, Metadata metadataCopy)
        {
            Debug.Assert(primaryModule != null);
            Debug.Assert(metadataCopy != null);
            ////Debug.Assert(!metadataCopy.IsImageOwner); property is now internal

            PrimaryModule = primaryModule;
            DocumentationFile = documentationFile;
            Metadata = metadataCopy;
        }

        // keep this internal so that users can't delete files that the provider manages
        internal void DisposeFileHandles()
        {
            PrimaryModule.DisposeFileStream();

            if (DocumentationFile != null)
            {
                DocumentationFile.DisposeFileStream();
            }
        }
    }
}
