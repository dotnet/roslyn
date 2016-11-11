// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
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
        private readonly IReferenceSerializationService _hostSerializationService;
        private readonly ConcurrentDictionary<string, IOptionsSerializationService> _lazyLanguageSerializationService;

        public Serializer(HostWorkspaceServices workspaceServices)
        {
            _workspaceServices = workspaceServices;
            _hostSerializationService = _workspaceServices.GetService<IReferenceSerializationService>();

            _lazyLanguageSerializationService = new ConcurrentDictionary<string, IOptionsSerializationService>(concurrencyLevel: 2, capacity: _workspaceServices.SupportedLanguages.Count());
        }

        public Checksum CreateChecksum(object value, CancellationToken cancellationToken)
        {
            var kind = value.GetWellKnownSynchronizationKind();

            using (Logger.LogBlock(FunctionId.Serializer_CreateChecksum, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (value is IChecksummedObject)
                {
                    return ((IChecksummedObject)value).Checksum;
                }

                switch (kind)
                {
                    case WellKnownSynchronizationKinds.Null:
                        return Checksum.Null;

                    case WellKnownSynchronizationKinds.CompilationOptions:
                    case WellKnownSynchronizationKinds.ParseOptions:
                    case WellKnownSynchronizationKinds.ProjectReference:
                        return Checksum.Create(value, kind, this);

                    case WellKnownSynchronizationKinds.MetadataReference:
                        return Checksum.Create(kind, _hostSerializationService.CreateChecksum((MetadataReference)value, cancellationToken));

                    case WellKnownSynchronizationKinds.AnalyzerReference:
                        return Checksum.Create(kind, _hostSerializationService.CreateChecksum((AnalyzerReference)value, cancellationToken));

                    case WellKnownSynchronizationKinds.SourceText:
                        return Checksum.Create(kind, new Checksum(((SourceText)value).GetChecksum()));

                    default:
                        // object that is not part of solution is not supported since we don't know what inputs are required to
                        // serialize it
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        public void Serialize(object value, ObjectWriter writer, CancellationToken cancellationToken)
        {
            var kind = value.GetWellKnownSynchronizationKind();

            using (Logger.LogBlock(FunctionId.Serializer_Serialize, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (value is ChecksumWithChildren)
                {
                    SerializeChecksumWithChildren((ChecksumWithChildren)value, writer, cancellationToken);
                    return;
                }

                switch (kind)
                {
                    case WellKnownSynchronizationKinds.Null:
                        // do nothing
                        return;

                    case WellKnownSynchronizationKinds.SolutionAttributes:
                    case WellKnownSynchronizationKinds.ProjectAttributes:
                    case WellKnownSynchronizationKinds.DocumentAttributes:
                        ((IObjectWritable)value).WriteTo(writer);
                        return;

                    case WellKnownSynchronizationKinds.CompilationOptions:
                        SerializeCompilationOptions((CompilationOptions)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKinds.ParseOptions:
                        SerializeParseOptions((ParseOptions)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKinds.ProjectReference:
                        SerializeProjectReference((ProjectReference)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKinds.MetadataReference:
                        SerializeMetadataReference((MetadataReference)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKinds.AnalyzerReference:
                        SerializeAnalyzerReference((AnalyzerReference)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKinds.SourceText:
                        SerializeSourceText(storage: null, text: (SourceText)value, writer: writer, cancellationToken: cancellationToken);
                        return;

                    default:
                        // object that is not part of solution is not supported since we don't know what inputs are required to
                        // serialize it
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        public T Deserialize<T>(string kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Serializer_Deserialize, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (kind)
                {
                    case WellKnownSynchronizationKinds.Null:
                        return default(T);

                    case WellKnownSynchronizationKinds.SolutionState:
                    case WellKnownSynchronizationKinds.ProjectState:
                    case WellKnownSynchronizationKinds.DocumentState:
                    case WellKnownSynchronizationKinds.Projects:
                    case WellKnownSynchronizationKinds.Documents:
                    case WellKnownSynchronizationKinds.TextDocuments:
                    case WellKnownSynchronizationKinds.ProjectReferences:
                    case WellKnownSynchronizationKinds.MetadataReferences:
                    case WellKnownSynchronizationKinds.AnalyzerReferences:
                        return (T)(object)DeserializeChecksumWithChildren(reader, cancellationToken);

                    case WellKnownSynchronizationKinds.SolutionAttributes:
                        return (T)(object)SolutionInfo.SolutionAttributes.ReadFrom(reader);
                    case WellKnownSynchronizationKinds.ProjectAttributes:
                        return (T)(object)ProjectInfo.ProjectAttributes.ReadFrom(reader);
                    case WellKnownSynchronizationKinds.DocumentAttributes:
                        return (T)(object)DocumentInfo.DocumentAttributes.ReadFrom(reader);
                    case WellKnownSynchronizationKinds.CompilationOptions:
                        return (T)(object)DeserializeCompilationOptions(reader, cancellationToken);
                    case WellKnownSynchronizationKinds.ParseOptions:
                        return (T)(object)DeserializeParseOptions(reader, cancellationToken);
                    case WellKnownSynchronizationKinds.ProjectReference:
                        return (T)(object)DeserializeProjectReference(reader, cancellationToken);
                    case WellKnownSynchronizationKinds.MetadataReference:
                        return (T)(object)DeserializeMetadataReference(reader, cancellationToken);
                    case WellKnownSynchronizationKinds.AnalyzerReference:
                        return (T)(object)DeserializeAnalyzerReference(reader, cancellationToken);
                    case WellKnownSynchronizationKinds.SourceText:
                        return (T)(object)DeserializeSourceText(reader, cancellationToken);
                    case WellKnownSynchronizationKinds.OptionSet:
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
