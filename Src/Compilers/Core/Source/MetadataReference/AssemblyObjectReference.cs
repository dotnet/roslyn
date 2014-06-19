using System;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Represents a reference to an in-memory Assembly object.
    /// </summary>
    public sealed class AssemblyObjectReference : MetadataReference
    {
        /// <summary>
        /// Returns the <see cref="System.Reflection.Assembly"/> object this reference uses.
        /// </summary>
        public System.Reflection.Assembly Assembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Creates an AssemblyObjectReference.
        /// </summary>
        /// <param name="assembly">The assembly to reference.</param>
        /// <param name="embedInteropTypes">Should interop types be embedded in the created assembly?</param>
        /// <param name="alias">A namespace alias for this reference.</param>
        public AssemblyObjectReference(System.Reflection.Assembly assembly, string alias = null, bool embedInteropTypes = false)
            : base(alias, embedInteropTypes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the kind of reference this is. This is useful for avoiding expensive type tests.
        /// </summary>
        internal override ReferenceKind Kind
        {
            get
            {
                return ReferenceKind.AssemblyObject;
            }
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}