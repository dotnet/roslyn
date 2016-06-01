// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// this represents hierarchical checksum of solution
    /// </summary>
    internal class SolutionSnapshotId : HierarchicalChecksumObject
    {
        public const string Name = nameof(SolutionSnapshotId);

        public readonly Checksum Info;
        public readonly SnapshotIdCollection<ProjectSnapshotId> Projects;

        internal SolutionSnapshotId(Serializer serializer, Checksum info, SnapshotIdCollection<ProjectSnapshotId> projects) :
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

    internal class ProjectSnapshotId : HierarchicalChecksumObject
    {
        public const string Name = nameof(ProjectSnapshotId);

        public readonly Checksum Info;
        public readonly Checksum CompilationOptions;
        public readonly Checksum ParseOptions;

        public readonly SnapshotIdCollection<DocumentSnapshotId> Documents;

        public readonly ChecksumCollection ProjectReferences;
        public readonly ChecksumCollection MetadataReferences;
        public readonly ChecksumCollection AnalyzerReferences;

        public readonly SnapshotIdCollection<DocumentSnapshotId> AdditionalDocuments;

        public ProjectSnapshotId(
            Serializer serializer,
            Checksum info, Checksum compilationOptions, Checksum parseOptions,
            SnapshotIdCollection<DocumentSnapshotId> documents,
            ChecksumCollection projectReferences,
            ChecksumCollection metadataReferences,
            ChecksumCollection analyzerReferences,
            SnapshotIdCollection<DocumentSnapshotId> additionalDocuments) :
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

    internal class DocumentSnapshotId : HierarchicalChecksumObject
    {
        public const string Name = nameof(DocumentSnapshotId);

        public readonly Checksum Info;
        public readonly Checksum Text;

        public DocumentSnapshotId(Serializer serializer, Checksum info, Checksum text) :
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

    internal class SnapshotIdCollection<T> : HierarchicalChecksumObject where T : ChecksumObject
    {
        public readonly ImmutableArray<T> Objects;

        public SnapshotIdCollection(Serializer serializer, ImmutableArray<T> objects, string kind) :
            base(serializer, Checksum.Create(kind, objects), kind)
        {
            Objects = objects;
        }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            return Serializer.SerializeAsync(this, writer, cancellationToken);
        }
    }

    internal class ChecksumCollection : HierarchicalChecksumObject
    {
        public readonly ImmutableArray<Checksum> Objects;

        public ChecksumCollection(Serializer serializer, ImmutableArray<Checksum> objects, string kind) :
            base(serializer, Checksum.Create(kind, objects, CancellationToken.None), kind)
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
