// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
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
        /// Indicates what kind of checksum object it is
        /// <see cref="WellKnownChecksumObjects"/> for examples.
        /// 
        /// this later will be used to deserialize bits to actual object
        /// </summary>
        public readonly string Kind;

        /// <summary>
        /// Checksum of this object
        /// </summary>
        public readonly Checksum Checksum;

        public ChecksumObject(Checksum checksum, string kind)
        {
            Checksum = checksum;
            Kind = kind;
        }

        /// <summary>
        /// This will write out this object's data (the data the checksum is associated with) to bits
        /// 
        /// this hide how each data is serialized to bits
        /// </summary>
        public abstract Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken);
    }

    /// <summary>
    /// <see cref="ChecksumObjectWithChildren"/>  indicates this type is collection of checksums.
    /// 
    /// <see cref="Asset"/> represents actual data (leaf node of hierarchical checksum tree) 
    /// </summary>
    internal abstract class ChecksumObjectWithChildren : ChecksumObject
    {
        private readonly Serializer _serializer;

        public ChecksumObjectWithChildren(Serializer serializer, string kind, params object[] children) :
            base(CreateChecksum(kind, children), kind)
        {
            _serializer = serializer;

            Children = children;
        }

        public object[] Children { get; }

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _serializer.SerializeChecksumObjectWithChildren(this, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }

        private static Checksum CreateChecksum(string kind, object[] children)
        {
            return Checksum.Create(kind, children.Select(c => c as Checksum ?? ((ChecksumCollection)c).Checksum));
        }
    }

    // TODO: Kind might not actually needed. see whether we can get rid of this
    internal static class WellKnownChecksumObjects
    {
        public const string Null = nameof(Null);

        public const string Projects = nameof(Projects);
        public const string Documents = nameof(Documents);
        public const string TextDocuments = nameof(TextDocuments);
        public const string ProjectReferences = nameof(ProjectReferences);
        public const string MetadataReferences = nameof(MetadataReferences);
        public const string AnalyzerReferences = nameof(AnalyzerReferences);

        public const string SolutionChecksumObjectInfo = nameof(SolutionChecksumObjectInfo);
        public const string ProjectChecksumObjectInfo = nameof(ProjectChecksumObjectInfo);
        public const string DocumentChecksumObjectInfo = nameof(DocumentChecksumObjectInfo);
        public const string CompilationOptions = nameof(CompilationOptions);
        public const string ParseOptions = nameof(ParseOptions);
        public const string ProjectReference = nameof(ProjectReference);
        public const string MetadataReference = nameof(MetadataReference);
        public const string AnalyzerReference = nameof(AnalyzerReference);
        public const string SourceText = nameof(SourceText);
        public const string OptionSet = nameof(OptionSet);
    }
}
