using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Represents a reference to an on-disk assembly file.
    /// </summary>
    [DebuggerDisplay("File: {Path,nq}")]
    public sealed class AssemblyFileReference : MetadataReference
    {
        /// <summary>
        /// Assembly file path as specified in the constructor.
        /// </summary>
        /// <remarks>
        /// If a relative path is specified in the constructor it is resolved via <see cref="FileResolver"/> when the 
        /// compilation is being compiled.
        /// </remarks>
        public string Path { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyFileReference"/> class.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="embedInteropTypes">if set to <c>true</c> interop types should be embedded in the created assembly.</param>
        /// <param name="alias">A namespace alias for this assembly reference.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        public AssemblyFileReference(
            string path,
            string alias = null,
            bool embedInteropTypes = false)
            : base(alias, embedInteropTypes)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("Path can't be empty", "path");
            }

            this.Path = path;
        }

        /// <summary>
        /// Gets the kind of reference this is. This is useful for avoiding expensive type tests.
        /// </summary>
        internal override ReferenceKind Kind
        {
            get
            {
                return ReferenceKind.AssemblyFile;
            }
        }

        // Note(EDMAURER) very naive implementations of equality. No attempt is made to determine if
        // two different names resolve to the same file on disk.

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return
                Hash.Combine(
                    this.CommonHashPart(),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(this.Path));
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as AssemblyFileReference);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The assembly to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(AssemblyFileReference obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            return
                this.CommonEquals(obj) &&
                StringComparer.OrdinalIgnoreCase.Equals(this.Path, obj.Path);
        }
    }
}
