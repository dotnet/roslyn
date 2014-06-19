using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents a reference to an in-memory Assembly object.
    /// </summary>
    public sealed class AssemblyObjectReference : MetadataReference
    {
        /// <summary>
        /// Returns the Assembly object this reference uses.
        /// </summary>
        public System.Reflection.Assembly Assembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Create an AssemblyObjectReference.
        /// </summary>
        /// <param name="assembly">The assembly to reference.</param>
        /// <param name="embedInteropTypes">Should interop types be embedded in the created assembly?</param>
        /// <param name="alias">A namespace alias for this reference.</param>
        public AssemblyObjectReference(System.Reflection.Assembly assembly, bool embedInteropTypes = false, string alias = null)
            : base(embedInteropTypes, alias)
        {
            throw new NotImplementedException();
        }

        internal override ReferenceKind Kind
        {
            get
            {
                return ReferenceKind.AssemblyObject;
            }
        }
    }
}
