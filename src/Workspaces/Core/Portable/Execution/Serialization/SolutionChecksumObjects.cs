// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// this represents hierarchical checksum of solution
    /// </summary>
    internal class SolutionChecksumObject : HierarchicalChecksumObject
    {
        public const string Name = nameof(SolutionChecksumObject);

        public readonly Checksum Info;
        public readonly ChecksumCollection Projects;

        internal SolutionChecksumObject(Serializer serializer, Checksum info, ChecksumCollection projects) :
            base(serializer, Checksum.Create(Name, info, projects.Checksum), Name)
        {
            Info = info;
            Projects = projects;
        }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            return Serializer.SerializeAsync(this, writer, cancellationToken);
        }
    }

    internal class ProjectChecksumObject : HierarchicalChecksumObject
    {
        public const string Name = nameof(ProjectChecksumObject);

        public readonly Checksum Info;
        public readonly Checksum CompilationOptions;
        public readonly Checksum ParseOptions;

        public readonly ChecksumCollection Documents;

        public readonly ChecksumCollection ProjectReferences;
        public readonly ChecksumCollection MetadataReferences;
        public readonly ChecksumCollection AnalyzerReferences;

        public readonly ChecksumCollection AdditionalDocuments;

        public ProjectChecksumObject(
            Serializer serializer,
            Checksum info, Checksum compilationOptions, Checksum parseOptions,
            ChecksumCollection documents,
            ChecksumCollection projectReferences,
            ChecksumCollection metadataReferences,
            ChecksumCollection analyzerReferences,
            ChecksumCollection additionalDocuments) :
            base(serializer, Checksum.Create(
                Name,
                info, compilationOptions, parseOptions,
                documents.Checksum, projectReferences.Checksum, metadataReferences.Checksum,
                analyzerReferences.Checksum, additionalDocuments.Checksum), Name)
        {
            Info = info;
            CompilationOptions = compilationOptions;
            ParseOptions = parseOptions;

            Documents = documents;

            ProjectReferences = projectReferences;
            MetadataReferences = metadataReferences;
            AnalyzerReferences = analyzerReferences;

            AdditionalDocuments = additionalDocuments;
        }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            return Serializer.SerializeAsync(this, writer, cancellationToken);
        }
    }

    internal class DocumentChecksumObject : HierarchicalChecksumObject
    {
        public const string Name = nameof(DocumentChecksumObject);

        public readonly Checksum Info;
        public readonly Checksum Text;

        public DocumentChecksumObject(Serializer serializer, Checksum info, Checksum text) :
            base(serializer, Checksum.Create(Name, info, text), Name)
        {
            Info = info;
            Text = text;
        }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            Serializer.Serialize(this, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }

    /// <summary>
    /// Collection of checksums of checksum objects.
    /// </summary>
    internal class ChecksumCollection : HierarchicalChecksumObject
    {
        public readonly ImmutableArray<Checksum> Objects;

        public ChecksumCollection(Serializer serializer, ImmutableArray<Checksum> objects, string kind) :
            base(serializer, Checksum.Create(kind, (IEnumerable<Checksum>)objects), kind)
        {
            Objects = objects;
        }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            Serializer.Serialize(this, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }
}
