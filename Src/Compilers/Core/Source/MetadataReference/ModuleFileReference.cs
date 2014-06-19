using System;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Represents a reference to an on-disk module file. This is the equivalent of the /addmodule command line option.
    /// </summary>
    public sealed class ModuleFileReference : MetadataReference
    {
        /// <summary>
        /// Module file path as specified in the constructor.
        /// </summary>
        /// <remarks>
        /// If a relative path is specified in the constructor it is resolved via <see cref="FileResolver"/> when the 
        /// compilation is being compiled.
        /// </remarks>
        public string Path { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="AssemblyFileReference"/> has been copied into memory as a snapshot.
        /// </summary>
        /// <value>
        ///   <c>true</c> if snapshot; otherwise, <c>false</c>.
        /// </value>
        public bool Snapshot { get; private set; }

        /// <summary>
        /// Creates a ModuleFileReference.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="snapshot">If true, the metadata is immediately copied into memory, so that this compilation is immune
        /// to changes to the referenced file. If false, 
        /// the file may be locked in memory or cause exceptions if it is changed on disk.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        public ModuleFileReference(
            string path,
            bool snapshot = false)
            : base(alias: null, embedInteropTypes: false)
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
            this.Snapshot = snapshot;
        }

        /// <summary>
        /// Gets the kind of reference this is. This is useful for avoiding expensive type tests.
        /// </summary>
        internal override ReferenceKind Kind
        {
            get
            {
                return ReferenceKind.ModuleFile;
            }
        }

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
                    this.Snapshot,
                    Hash.Combine(
                        this.CommonHashPart(),
                        StringComparer.OrdinalIgnoreCase.GetHashCode(this.Path)));
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
            return this.Equals(obj as ModuleFileReference);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The assembly to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ModuleFileReference obj)
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
                StringComparer.OrdinalIgnoreCase.Equals(this.Path, obj.Path) &&
                this.Snapshot == obj.Snapshot;
        }
    }
}