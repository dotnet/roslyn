// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    public class ProjectState
    {
        public ProjectState(string name, string language, string defaultPrefix, string defaultExtension)
        {
            Name = name;
            Language = language;
            DefaultPrefix = defaultPrefix;
            DefaultExtension = defaultExtension;

            Sources = new SourceFileList(defaultPrefix, defaultExtension);
        }

        public string Name { get; }

        public string AssemblyName => Name;

        public string Language { get; }

        /// <summary>
        /// Gets the set of source files for analyzer or code fix testing. Files may be added to this list using one of
        /// the <see cref="SourceFileList.Add(string)"/> methods.
        /// </summary>
        public SourceFileList Sources { get; }

        public MetadataReferenceCollection AdditionalReferences { get; } = new MetadataReferenceCollection();

        private protected string DefaultPrefix { get; }

        private protected string DefaultExtension { get; }
    }
}
