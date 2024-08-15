// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

using static TemporaryStorageService;

internal partial class SerializerService
{
    /// <summary>
    /// No methods on this serialized type should be called.  It exists as a placeholder to allow the data to be
    /// transmitted over to the remote site.  On the remote site we will first collect *all* of these serialized
    /// analyzer references, then create the actual <see cref="AnalyzerFileReference"/>s in their own safe
    /// AssemblyLoadContext distinct from everything else.
    /// </summary>
    public sealed class SerializedAnalyzerReference(string fullPath) : AnalyzerReference
    {
        public override string? FullPath { get; } = fullPath;

        public override object Id
            => throw new InvalidOperationException();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            => throw new InvalidOperationException();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            => throw new InvalidOperationException();

        [Obsolete]
        public override ImmutableArray<ISourceGenerator> GetGenerators()
            => throw new InvalidOperationException();

        public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
            => throw new InvalidOperationException();

        public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages()
            => throw new InvalidOperationException();
    }

    [DebuggerDisplay("{" + nameof(Display) + ",nq}")]
    private sealed class SerializedMetadataReference : PortableExecutableReference, ISupportTemporaryStorage
    {
        private readonly Metadata _metadata;
        private readonly ImmutableArray<TemporaryStorageStreamHandle> _storageHandles;
        private readonly DocumentationProvider _provider;

        public IReadOnlyList<ITemporaryStorageStreamHandle> StorageHandles => _storageHandles;

        public SerializedMetadataReference(
            MetadataReferenceProperties properties,
            string? fullPath,
            Metadata metadata,
            ImmutableArray<TemporaryStorageStreamHandle> storageHandles,
            DocumentationProvider initialDocumentation)
            : base(properties, fullPath, initialDocumentation)
        {
            Contract.ThrowIfTrue(storageHandles.IsDefault);
            _metadata = metadata;
            _storageHandles = storageHandles;

            _provider = initialDocumentation;
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            // this uses documentation provider given at the constructor
            throw ExceptionUtilities.Unreachable();
        }

        protected override Metadata GetMetadataImpl()
            => _metadata;

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            => new SerializedMetadataReference(properties, FilePath, _metadata, _storageHandles, _provider);

        public override string ToString()
        {
            var metadata = TryGetMetadata(this);
            var modules = GetModules(metadata);

            return $"""
            {nameof(SerializedMetadataReference)}
                FilePath={this.FilePath}
                Kind={this.Properties.Kind}
                Aliases={this.Properties.Aliases.Join(",")}
                EmbedInteropTypes={this.Properties.EmbedInteropTypes}
                MetadataKind={metadata switch { null => "null", AssemblyMetadata => "assembly", ModuleMetadata => "module", _ => metadata.GetType().Name }}
                Guids={modules.Select(m => GetMetadataGuid(m).ToString()).Join(",")}
            """;

            static ImmutableArray<ModuleMetadata> GetModules(Metadata? metadata)
            {
                if (metadata is AssemblyMetadata assemblyMetadata)
                {
                    if (TryGetModules(assemblyMetadata, out var modules))
                        return modules;
                }
                else if (metadata is ModuleMetadata moduleMetadata)
                {
                    return [moduleMetadata];
                }

                return [];
            }
        }
    }
}
