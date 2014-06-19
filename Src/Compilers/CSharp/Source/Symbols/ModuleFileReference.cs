using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents a reference to an on-disk module file. This is the equivalent of the /addmodule command line option.
    /// </summary>
    public sealed class ModuleFileReference : MetadataReference
    {
        private readonly bool snapshot;
        private readonly string fullFileName;

        /// <summary>
        /// Returns the file name this reference uses.
        /// </summary>
        public string FileName
        {
            get
            {
                return fullFileName;
            }
        }

        public bool Snapshot
        {
            get
            {
                return snapshot;
            }
        }

        /// <summary>
        /// Create an ModuleFileReference. 
        /// </summary>
        /// <param name="fileName">The file to reference.</param>
        /// <param name="snapshot">If true, the metadata is immediate snapshotted into memory, so that this compilation is immune
        /// to changes to the referenced file. If false, 
        /// the file may be locked in memory or cause exceptions if it is changed on disk.</param>
        /// <param name="alias">A namespace alias for this reference.</param>
        public ModuleFileReference(
            string fileName,
            bool snapshot = false,
            string alias = null)
            : base(false, alias)
        {
            Contract.ThrowIfFalse(fileName != null && fileName.Length > 0);

            this.snapshot = snapshot;
            this.fullFileName = System.IO.Path.GetFullPath(fileName);
        }

        internal override ReferenceKind Kind
        {
            get
            {
                return ReferenceKind.ModuleFile;
            }
        }
    }
}