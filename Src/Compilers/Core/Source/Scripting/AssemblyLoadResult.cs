using System;

namespace Roslyn.Scripting
{
    /// <summary>
    /// The result of loading an assembly reference to the interactive session.
    /// </summary>
    [Serializable]
    public struct AssemblyLoadResult
    {
        private readonly string path;
        private readonly string originalPath;
        private readonly bool successful;

        internal static AssemblyLoadResult CreateSuccessful(string path, string originalPath)
        {
            return new AssemblyLoadResult(path, originalPath, successful: true);
        }

        internal static AssemblyLoadResult CreateAlreadyLoaded(string path, string originalPath)
        {
            return new AssemblyLoadResult(path, originalPath, successful: false);
        }
        
        private AssemblyLoadResult(string path, string originalPath, bool successful)
        {
            this.path = path;
            this.originalPath = originalPath;
            this.successful = successful;
        }

        /// <summary>
        /// True if the assembly was loaded by the assembly loader, false if has been loaded before.
        /// </summary>
        public bool IsSuccessful
        {
            get { return successful; }
        }

        /// <summary>
        /// Full path to the physical assembly file (might be a shadow-copy of the original assembly file).
        /// </summary>
        public string Path
        {
            get { return path; }
        }

        /// <summary>
        /// Original assembly file path.
        /// </summary>
        public string OriginalPath
        {
            get { return originalPath; }
        }
    }
}
