// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Base for object with checksum
    /// </summary>
    internal abstract class ChecksumObject
    {
        /// <summary>
        /// indicate waht kind of checksum object it is
        /// <see cref="WellKnownChecksumObjects"/> for examples.
        /// </summary>
        public readonly string Kind;
        public readonly Checksum Checksum;

        public ChecksumObject(Checksum checksum, string kind)
        {
            Checksum = checksum;
            Kind = kind;
        }

        public abstract Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken);
    }

    // TODO: Kind might not actually needed. see whether we can get rid of this
    internal static class WellKnownChecksumObjects
    {
        public const string Projects = nameof(Projects);
        public const string Documents = nameof(Documents);
        public const string TextDocuments = nameof(TextDocuments);
        public const string ProjectReferences = nameof(ProjectReferences);
        public const string MetadataReferences = nameof(MetadataReferences);
        public const string AnalyzerReferences = nameof(AnalyzerReferences);

        public const string SolutionSnapshotInfo = nameof(SolutionSnapshotInfo);
        public const string ProjectSnapshotInfo = nameof(ProjectSnapshotInfo);
        public const string DocumentSnapshotInfo = nameof(DocumentSnapshotInfo);
        public const string CompilationOptions = nameof(CompilationOptions);
        public const string ParseOptions = nameof(ParseOptions);
        public const string ProjectReference = nameof(ProjectReference);
        public const string MetadataReference = nameof(MetadataReference);
        public const string AnalyzerReference = nameof(AnalyzerReference);
        public const string SourceText = nameof(SourceText);
    }
}
