// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

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

        internal ProjectState(ProjectState sourceState)
        {
            Name = sourceState.Name;
            Language = sourceState.Language;
            ReferenceAssemblies = sourceState.ReferenceAssemblies;
            OutputKind = sourceState.OutputKind;
            DocumentationMode = sourceState.DocumentationMode;
            DefaultPrefix = sourceState.DefaultPrefix;
            DefaultExtension = sourceState.DefaultExtension;
            Sources = new SourceFileList(DefaultPrefix, DefaultExtension);

            Sources.AddRange(sourceState.Sources);
            GeneratedSources.AddRange(sourceState.GeneratedSources);
            AdditionalFiles.AddRange(sourceState.AdditionalFiles);
            AnalyzerConfigFiles.AddRange(sourceState.AnalyzerConfigFiles);
            AdditionalFilesFactories.AddRange(sourceState.AdditionalFilesFactories);
            AdditionalProjectReferences.AddRange(sourceState.AdditionalProjectReferences);
        }

        public string Name { get; }

        public string AssemblyName => Name;

        public string Language { get; }

        /// <summary>
        /// Gets or sets the reference assemblies to use for the project.
        /// </summary>
        /// <value>
        /// A <see cref="Testing.ReferenceAssemblies"/> instance to use specific reference assemblies; otherwise,
        /// <see langword="null"/> to inherit the reference assemblies from
        /// <see cref="AnalyzerTest{TVerifier}.ReferenceAssemblies"/>.
        /// </value>
        public ReferenceAssemblies? ReferenceAssemblies { get; set; }

        public OutputKind? OutputKind { get; set; }

        public DocumentationMode? DocumentationMode { get; set; }

        /// <summary>
        /// Gets the set of source files for analyzer or code fix testing. Files may be added to this list using one of
        /// the <see cref="SourceFileList.Add(string)"/> methods.
        /// </summary>
        public SourceFileList Sources { get; }

        public SourceFileCollection GeneratedSources { get; } = new SourceFileCollection();

        public SourceFileCollection AdditionalFiles { get; } = new SourceFileCollection();

        public SourceFileCollection AnalyzerConfigFiles { get; } = new SourceFileCollection();

        public List<Func<IEnumerable<(string filename, SourceText content)>>> AdditionalFilesFactories { get; } = new List<Func<IEnumerable<(string filename, SourceText content)>>>();

        public List<string> AdditionalProjectReferences { get; } = new List<string>();

        public MetadataReferenceCollection AdditionalReferences { get; } = new MetadataReferenceCollection();

        private protected string DefaultPrefix { get; }

        private protected string DefaultExtension { get; }
    }
}
