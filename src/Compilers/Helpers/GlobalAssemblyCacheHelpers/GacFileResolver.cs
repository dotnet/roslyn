using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Scripting
{
    /// <summary>
    /// Extends <see cref="MetadataFileReferenceResolver"/> to enable resolution of assembly
    /// simple names in the GAC.
    /// </summary>
    internal sealed class GacFileResolver : MetadataFileReferenceResolver
    {
        private readonly ImmutableArray<ProcessorArchitecture> architectures;
        private readonly CultureInfo preferredCulture;

        /// <summary>
        /// A resolver that is configured to resolve against the GAC associated
        /// with the bitness of the currently executing process.
        /// </summary>
        internal new static GacFileResolver Default = new GacFileResolver(
            assemblySearchPaths: ImmutableArray<string>.Empty,
            baseDirectory: null,
            architectures: GlobalAssemblyCache.CurrentArchitectures,
            preferredCulture: null);

        /// <summary>
        /// Constructs an instance of a <see cref="GacFileResolver"/>
        /// </summary>
        /// <param name="assemblySearchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        /// <param name="architectures">Supported architectures used to filter GAC assemblies.</param>
        /// <param name="preferredCulture">A culture to use when choosing the best assembly from 
        /// among the set filtered by <paramref name="architectures"/></param>
        public GacFileResolver(
            IEnumerable<string> assemblySearchPaths,
            string baseDirectory,
            ImmutableArray<ProcessorArchitecture> architectures,
            CultureInfo preferredCulture)
            : base(assemblySearchPaths, baseDirectory)
        {
            this.architectures = architectures;
            this.preferredCulture = preferredCulture;
        }

        /// <summary>
        /// Architecture filter used when resolving assembly references.
        /// </summary>
        public ImmutableArray<ProcessorArchitecture> Architectures
        {
            get { return architectures; }
        }

        /// <summary>
        /// CultureInfo used when resolving assembly references.
        /// </summary>
        public CultureInfo PreferredCulture
        {
            get { return preferredCulture; }
        }

        public override string ResolveReference(string reference, string baseFilePath)
        {
            if (PathUtilities.IsFilePath(reference))
            {
                return base.ResolveReference(reference, baseFilePath);
            }

            string path;
            GlobalAssemblyCache.ResolvePartialName(reference, out path, this.architectures, this.PreferredCulture);
            return FileExists(path) ? path : null;
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            var other = (GacFileResolver)obj;
            return this.architectures.SequenceEqual(other.architectures) &&
                this.preferredCulture == other.preferredCulture;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(base.GetHashCode(),
                   Hash.Combine(this.preferredCulture, Hash.CombineValues(this.architectures)));
        }
    }
}
