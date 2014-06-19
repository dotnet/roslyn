using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents a reference to an on-disk assembly file. 
    /// </summary>
    public sealed class AssemblyFileReference : MetadataReference
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
        /// Create an AssemblyFileReference.
        /// </summary>
        /// <param name="fileName">The file to reference.</param>
        /// <param name="snapshot">If true, the metadata is immediate snapshotted into memory, so that this compilation is immune
        /// to changes to the referenced file. If false, 
        /// the file may be locked in memory or cause exceptions if it is changed on disk.</param>
        /// <param name="embedInteropTypes">Should interop types be embedded in the created assembly?</param>
        /// <param name="alias">A namespace alias for this reference.</param>
        public AssemblyFileReference(
            string fileName,
            bool snapshot = false,
            bool embedInteropTypes = false,
            string alias = null)
            : base(embedInteropTypes, alias)
        {
            if (fileName == null)
            {
                throw new NullReferenceException();
            }

            if (fileName.Length == 0)
            {
                throw new ArgumentOutOfRangeException("fileName");
            }

            this.snapshot = snapshot;
            this.fullFileName = System.IO.Path.GetFullPath(fileName);
        }

        internal override ReferenceKind Kind
        {
            get
            {
                return ReferenceKind.AssemblyFile;
            }
        }
    }
}