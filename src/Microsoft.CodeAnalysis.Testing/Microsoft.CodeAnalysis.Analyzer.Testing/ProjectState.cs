// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class ProjectState
    {
        private readonly string _defaultPrefix;
        private readonly string _defaultExtension;

        protected ProjectState(string name, string defaultPrefix, string defaultExtension)
        {
            Name = name;
            _defaultPrefix = defaultPrefix;
            _defaultExtension = defaultExtension;

            Sources = new SourceFileList(defaultPrefix, defaultExtension);
        }

        public string Name { get; }

        public string AssemblyName => Name;

        public abstract string Language { get; }

        /// <summary>
        /// Gets the set of source files for analyzer or code fix testing. Files may be added to this list using one of
        /// the <see cref="SourceFileList.Add(string)"/> methods.
        /// </summary>
        public SourceFileList Sources { get; }

        public MetadataReferenceCollection AdditionalReferences { get; } = new MetadataReferenceCollection();
    }
}
