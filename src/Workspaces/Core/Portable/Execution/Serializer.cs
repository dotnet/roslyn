// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
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
        private readonly ConcurrentDictionary<string, IOptionsSerializationService> _lazyLanguageSerializationService;

        public readonly IReferenceSerializationService HostSerializationService;

        public Serializer(HostWorkspaceServices workspaceServices)
        {
            _workspaceServices = workspaceServices;

            HostSerializationService = _workspaceServices.GetService<IReferenceSerializationService>();
            _lazyLanguageSerializationService = new ConcurrentDictionary<string, IOptionsSerializationService>(concurrencyLevel: 2, capacity: _workspaceServices.SupportedLanguages.Count());

            // TODO: figure out how to support Serialize like the way Deserialize work. tried once, couldn't figure out since
            //       different kind of data require different number of data to serialize it. that is required so that we don't hold on
            //       to any red node.
        }

        public T Deserialize<T>(string kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Serializer_Deserialize, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (kind)
                {
                    case WellKnownChecksumObjects.Null:
                        return default(T);

                    case SolutionChecksumObject.Name:
                        return (T)(object)DeserializeChecksumObjectWithChildren(reader, cancellationToken);
                    case ProjectChecksumObject.Name:
                        return (T)(object)DeserializeChecksumObjectWithChildren(reader, cancellationToken);
                    case DocumentChecksumObject.Name:
                        return (T)(object)DeserializeChecksumObjectWithChildren(reader, cancellationToken);

                    case WellKnownChecksumObjects.Projects:
                    case WellKnownChecksumObjects.Documents:
                    case WellKnownChecksumObjects.TextDocuments:
                    case WellKnownChecksumObjects.ProjectReferences:
                    case WellKnownChecksumObjects.MetadataReferences:
                    case WellKnownChecksumObjects.AnalyzerReferences:
                        return (T)(object)DeserializeChecksumObjectWithChildren(reader, cancellationToken);

                    case WellKnownChecksumObjects.SolutionChecksumObjectInfo:
                        return (T)(object)DeserializeSolutionChecksumObjectInfo(reader, cancellationToken);
                    case WellKnownChecksumObjects.ProjectChecksumObjectInfo:
                        return (T)(object)DeserializeProjectChecksumObjectInfo(reader, cancellationToken);
                    case WellKnownChecksumObjects.DocumentChecksumObjectInfo:
                        return (T)(object)DeserializeDocumentChecksumObjectInfo(reader, cancellationToken);
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
                    case WellKnownChecksumObjects.OptionSet:
                        return (T)(object)DeserializeOptionSet(reader, cancellationToken);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        private IOptionsSerializationService GetOptionsSerializationService(string languageName)
        {
            return _lazyLanguageSerializationService.GetOrAdd(languageName, n => _workspaceServices.GetLanguageServices(n).GetService<IOptionsSerializationService>());
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
