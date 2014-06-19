using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Represents a reference to an in-memory assembly image.
    /// </summary>
    [DebuggerDisplay("{ToDebugString()}")]
    public sealed class AssemblyBytesReference : MetadataReference
    {
        /// <summary>
        /// The bytes for this assembly reference.
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// A string that uniquely identifies this reference within the current AppDomain. Two references that have the same unique name are interchangeable.
        /// They must therefore point to equivalent images (byte[]).
        /// </summary>
        public string UniqueName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyBytesReference"/> class.
        /// </summary>
        /// <param name="bytes">The bytes representing this assembly</param>
        /// <param name="uniqueName">Unique name of this assembly or null, in which case a GUID is generated.</param>
        /// <param name="embedInteropTypes">If set to <c>true</c> interop types should be embedded in the created
        /// assembly.</param>
        /// <param name="alias">A namespace alias for this assembly reference.</param>
        ///
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="uniqueName"/> is null.</exception>
        public AssemblyBytesReference(
            byte[] bytes,
            string uniqueName = null,
            string alias = null,
            bool embedInteropTypes = false)
            : base(alias, embedInteropTypes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (uniqueName == null)
            {
                uniqueName = Guid.NewGuid().ToString();
            }

            // we copy the byte[] later when loading the reference into the MetadataCache:
            this.Bytes = bytes;
            this.UniqueName = uniqueName;
        }

        /// <summary>
        /// Gets the kind of reference this is. This is useful for avoiding expensive type tests.
        /// </summary>
        internal override ReferenceKind Kind
        {
            get
            {
                return ReferenceKind.AssemblyBytes;
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
                Hash.Combine(this.UniqueName, this.CommonHashPart());
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
            return this.Equals(obj as AssemblyBytesReference);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The assembly to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(AssemblyBytesReference obj)
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
                this.UniqueName == obj.UniqueName;
        }

        /// <summary>
        /// Creates the debug string for this assembly.
        /// </summary>
        /// <returns>The debug string</returns>
        internal string ToDebugString()
        {
            return string.Format("AssemblyBytesReference({0})", UniqueName);
        }
    }
}
