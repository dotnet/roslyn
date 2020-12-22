// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing.Model
{
    /// <summary>
    /// Represents an evaluated <see cref="ProjectState"/>.
    /// </summary>
    public sealed class EvaluatedProjectState
    {
        public EvaluatedProjectState(ProjectState state, ReferenceAssemblies defaultReferenceAssemblies)
        {
            Name = state.Name;
            AssemblyName = state.AssemblyName;
            Language = state.Language;
            ReferenceAssemblies = state.ReferenceAssemblies ?? defaultReferenceAssemblies;
            OutputKind = state.OutputKind;
            DocumentationMode = state.DocumentationMode;
            Sources = state.Sources.ToImmutableArray();
            AdditionalFiles = state.AdditionalFiles.ToImmutableArray();
            AdditionalProjectReferences = state.AdditionalProjectReferences.ToImmutableArray();
            AdditionalReferences = state.AdditionalReferences.ToImmutableArray();
        }

        public string Name { get; }

        public string AssemblyName { get; }

        public string Language { get; }

        public ReferenceAssemblies ReferenceAssemblies { get; }

        public OutputKind OutputKind { get; }

        public DocumentationMode DocumentationMode { get; }

        public ImmutableArray<(string filename, SourceText content)> Sources { get; }

        public ImmutableArray<(string filename, SourceText content)> AdditionalFiles { get; }

        public ImmutableArray<string> AdditionalProjectReferences { get; }

        public ImmutableArray<MetadataReference> AdditionalReferences { get; }
    }
}
