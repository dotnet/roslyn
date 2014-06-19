using System;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Represents a reference to an on-disk assembly file, or a stream in memory.
    /// </summary>
    [DebuggerDisplay("Name: {Name,nq}")]
    public sealed class AssemblyNameReference : MetadataReference
    {
        /// <summary>
        /// Assembly name. An arbitrary string processed by <see cref="FileResolver"/>.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Creates a <see cref="AssemblyNameReference"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the assembly to reference. The name is resolved when the reference is consumed by the compiler.
        /// </param>
        /// <param name="alias">A namespace alias for this reference.</param>
        /// <param name="embedInteropTypes">If set to <c>true</c> interop types should be embedded in the created assembly.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public AssemblyNameReference(
            string name,
            string alias = null,
            bool embedInteropTypes = false)
            : base(alias, embedInteropTypes)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("Name cannot be empty.", "name");
            }

            this.Name = name;
        }

        /// <summary>
        /// Gets the kind of reference this is. This is useful for avoiding expensive type tests.
        /// </summary>
        internal override ReferenceKind Kind
        {
            get { return ReferenceKind.AssemblyName; }
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
                    this.CommonHashPart(),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name));
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public sealed override bool Equals(object obj)
        {
            return this.Equals(obj as AssemblyNameReference);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The assembly to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(AssemblyNameReference obj)
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
                StringComparer.OrdinalIgnoreCase.Equals(this.Name, obj.Name);
        }
    }
}
