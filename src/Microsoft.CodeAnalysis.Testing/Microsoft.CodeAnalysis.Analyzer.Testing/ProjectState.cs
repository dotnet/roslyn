// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
