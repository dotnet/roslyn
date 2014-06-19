using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    // TODO: Add MetadataReference have metadata reference.

    /// <summary>
    /// Represents a MetadataReference that is provided to a Compilation. There are several different types
    /// of metadata reference that can be provided.
    /// </summary>
    public abstract class MetadataReference
    {
        private readonly string alias;
        private readonly bool embedInteropTypes;

        public string Alias
        {
            get
            {
                return alias;
            }
        }

        public bool EmbedInteropTypes
        {
            get
            {
                return embedInteropTypes;
            }
        }

        // External clients cannot created new subclasses.
        internal MetadataReference(bool embedInteropTypes, string alias)
        {
            this.alias = alias;
            this.embedInteropTypes = embedInteropTypes;
        }

        /// <summary>
        /// Get the kind of reference this is. This is useful for avoiding expensive type tests.
        /// </summary>
        internal abstract ReferenceKind Kind { get; }
    }

    // TODO: Do we need the equivalent of AssemblyFileReference, but loading from a Stream or a byte[]? 

    /// <summary>
    /// The possible kinds of references.
    /// </summary>
    internal enum ReferenceKind
    {
        AssemblyObject,
        AssemblyFile,
        ModuleFile,
        Compilation
    }
}