// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// serialize and deserialize objects to straem.
    /// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
    /// 
    /// also, consider moving this serializer to use C# BOND serializer 
    /// https://github.com/Microsoft/bond
    /// </summary>
    internal partial class Serializer
    {
        private readonly HostWorkspaceServices _workspaceServices;
        private readonly ConcurrentDictionary<string, ILanguageSpecificSerializationService> _lazyLanguageSerializationService;

        public readonly IReferenceSerializationService HostSerializationService;

        public Serializer(HostWorkspaceServices workspaceServices)
        {
            _workspaceServices = workspaceServices;

            HostSerializationService = _workspaceServices.GetService<IReferenceSerializationService>();
            _lazyLanguageSerializationService = new ConcurrentDictionary<string, ILanguageSpecificSerializationService>(concurrencyLevel: 2, capacity: _workspaceServices.SupportedLanguages.Count());
        }

        public T Deserialize<T>(string kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (kind)
            {
                case SolutionSnapshotId.Name:
                    return (T)(object)DeserializeSolutionSnapshotId(reader, cancellationToken);
                case ProjectSnapshotId.Name:
                    return (T)(object)DeserializeProjectSnapshotId(reader, cancellationToken);
                case DocumentSnapshotId.Name:
                    return (T)(object)DeserializeDocumentSnapshotId(reader, cancellationToken);

                case WellKnownChecksumObjects.Projects:
                    return (T)(object)DeserializeSnapshotIdCollection<ProjectSnapshotId>(reader, cancellationToken);
                case WellKnownChecksumObjects.Documents:
                case WellKnownChecksumObjects.TextDocuments:
                    return (T)(object)DeserializeSnapshotIdCollection<DocumentSnapshotId>(reader, cancellationToken);
                case WellKnownChecksumObjects.ProjectReferences:
                case WellKnownChecksumObjects.MetadataReferences:
                case WellKnownChecksumObjects.AnalyzerReferences:
                    return (T)(object)DeserializeChecksumCollection(reader, cancellationToken);

                case WellKnownChecksumObjects.SolutionSnapshotInfo:
                    return (T)(object)DeserializeSolutionSnapshotInfo(reader, cancellationToken);
                case WellKnownChecksumObjects.ProjectSnapshotInfo:
                    return (T)(object)DeserializeProjectSnapshotInfo(reader, cancellationToken);
                case WellKnownChecksumObjects.DocumentSnapshotInfo:
                    return (T)(object)DeserializeDocumentSnapshotInfo(reader, cancellationToken);
                case WellKnownChecksumObjects.CompilationOptions:
                    return (T)(object)DeserializeCompilationOptions(reader, cancellationToken);
                case WellKnownChecksumObjects.ParseOptions:
                    return (T)(object)DeserializeParseOptions(reader, cancellationToken);
                case WellKnownChecksumObjects.ProjectReference:
                    return (T)(object)DeserializeProjectReference(reader, cancellationToken);
                case WellKnownChecksumObjects.MetadataReference:
                    return (T)(object)DeserializeMetadataReference(reader, cancellationToken);
                case WellKnownChecksumObjects.AnalyzerReference:
                    return (T)(object)DeserializeAnalyzerReference(reader, cancellationToken);
                case WellKnownChecksumObjects.SourceText:
                    return (T)(object)DeserializeSourceText(reader, cancellationToken);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private ILanguageSpecificSerializationService GetSerializationService(string languageName)
        {
            return _lazyLanguageSerializationService.GetOrAdd(languageName, n => _workspaceServices.GetLanguageServices(n).GetService<ILanguageSpecificSerializationService>());
        }
    }

    // TODO: convert this to sub class rather than using enum with if statement.
    internal enum SerializationKinds
    {
        Bits,
        FilePath,
        MemoryMapFile
    }
}
