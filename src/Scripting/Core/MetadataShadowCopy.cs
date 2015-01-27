// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a shadow copy of an assembly or a standalone module.
    /// </summary>
    public sealed class MetadataShadowCopy
    {
        private readonly ShadowCopy _primaryModule;
        private readonly ShadowCopy _documentationFile;

        // this instance doesn't own the image
        private readonly Metadata _metadataCopy;

        internal MetadataShadowCopy(ShadowCopy primaryModule, ShadowCopy documentationFile, Metadata metadataCopy)
        {
            Debug.Assert(primaryModule != null);
            Debug.Assert(metadataCopy != null);
            ////Debug.Assert(!metadataCopy.IsImageOwner); property is now internal

            _primaryModule = primaryModule;
            _documentationFile = documentationFile;
            _metadataCopy = metadataCopy;
        }

        /// <summary>
        /// Assembly manifest module copy or a standalone module copy.
        /// </summary>
        public ShadowCopy PrimaryModule
        {
            get { return _primaryModule; }
        }

        /// <summary>
        /// Documentation file copy or null if there is none.
        /// </summary>
        public ShadowCopy DocumentationFile
        {
            get { return _documentationFile; }
        }

        public Metadata Metadata
        {
            get { return _metadataCopy; }
        }

        // keep this internal so that users can't delete files that the provider manages
        internal void DisposeFileHandles()
        {
            _primaryModule.DisposeFileStream();

            if (_documentationFile != null)
            {
                _documentationFile.DisposeFileStream();
            }
        }
    }
}
