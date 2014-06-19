using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using Roslyn.Compilers;

namespace Roslyn.Scripting
{
    /// <summary>
    /// Represents a shadow copy of an assembly or a standalone module.
    /// </summary>
    public sealed class MetadataShadowCopy
    {
        private readonly ShadowCopy primaryModule;
        private readonly ShadowCopy documentationFile;

        // this instance doesn't own the image
        private readonly Metadata metadataCopy;

        internal MetadataShadowCopy(ShadowCopy primaryModule, ShadowCopy documentationFile, Metadata metadataCopy)
        {
            Debug.Assert(primaryModule != null);
            Debug.Assert(metadataCopy != null);
            Debug.Assert(!metadataCopy.IsImageOwner);

            this.primaryModule = primaryModule;
            this.documentationFile = documentationFile;
            this.metadataCopy = metadataCopy;
        }

        /// <summary>
        /// Assembly manifest module copy or a standalone module copy.
        /// </summary>
        public ShadowCopy PrimaryModule
        {
            get { return primaryModule; }
        }

        /// <summary>
        /// Documentation file copy or null if there is none.
        /// </summary>
        public ShadowCopy DocumentationFile
        {
            get { return documentationFile; }
        }

        public Metadata Metadata
        {
            get { return metadataCopy; }
        }

        // keep this internal so that users can't delete files that the provider manages
        internal void DisposeFileHandles()
        {
            primaryModule.DisposeFileStream();

            if (documentationFile != null)
            {
                documentationFile.DisposeFileStream();
            }
        }
    }
}
