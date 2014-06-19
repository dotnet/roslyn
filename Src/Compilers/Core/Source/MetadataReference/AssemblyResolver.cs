using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Resolves assemblies in GAC and on given search paths.
    /// </summary>
    /// <remarks>
    /// This object is immutable.
    /// </remarks>
    public sealed class AssemblyResolver : MetadataReferenceResolver
    {
        private readonly Func<ProcessorArchitecture, bool> architectureFilter;
        private readonly CultureInfo preferredCulture;
        private readonly ReadOnlyArray<string> searchPaths;
        private readonly Func<string, string> getFullPath;

        public AssemblyResolver()
            : this(ReadOnlyArray<string>.Empty)
        {
        }

        public AssemblyResolver(ReadOnlyArray<string> searchPaths)
            : this(searchPaths, null, GlobalAssemblyCache.CurrentArchitectureFilter, CultureInfo.CurrentCulture)
        {
        }

        public AssemblyResolver(
            ReadOnlyArray<string> searchPaths,
            Func<string, string> getFullPath,
            Func<ProcessorArchitecture, bool> architectureFilter,
            CultureInfo preferredCulture)
        {
            Contract.ThrowIfTrue(searchPaths.IsNull);
            Contract.ThrowIfTrue(searchPaths.Any(p => string.IsNullOrEmpty(p)));

            this.searchPaths = searchPaths;
            this.getFullPath = getFullPath ?? FileUtilities.TryGetFullPath;
            this.architectureFilter = architectureFilter;
            this.preferredCulture = preferredCulture;
        }

        public AssemblyResolver WithPaths(ReadOnlyArray<string> paths)
        {
            if (paths == this.searchPaths)
            {
                return this;
            }

            return new AssemblyResolver(paths, getFullPath, architectureFilter, preferredCulture);
        }

        public ReadOnlyArray<string> SearchPaths
        {
            get { return searchPaths; }
        }

        public Func<ProcessorArchitecture, bool> ArchitectureFilter
        {
            get { return architectureFilter; }
        }

        public CultureInfo PreferredCulture
        {
            get { return preferredCulture; }
        }

        public string ResolveAssemblyName(string displayName)
        {
            Contract.ThrowIfNull(displayName);
            AssemblyIdentity identity = GlobalAssemblyCache.ResolvePartialName(displayName, architectureFilter, preferredCulture);
            return (identity != null) ? identity.Location : null;
        }

        public override MetadataReference ResolveAssemblyName(AssemblyNameReference assemblyName)
        {
            string path = ResolveAssemblyName(assemblyName.Name);
            if (path != null)
            {
                return new AssemblyFileReference(path, assemblyName.Alias, assemblyName.EmbedInteropTypes);
            }

            return base.ResolveAssemblyName(assemblyName);
        }

        // internal for testing
        internal string ResolvePath(string path, string basePath, Func<string, bool> fileExists)
        {
            bool searchPathsUsed;
            string result = FileUtilities.ResolveRelativePath(path, basePath, searchPaths.AsEnumerable(), getFullPath, fileExists, out searchPathsUsed);

            if (!fileExists(result))
            {
                return null;
            }

            return result;
        }

        public override string ResolvePath(string path, string basePath)
        {
            return ResolvePath(path, basePath, File.Exists);
        }
    }
}
