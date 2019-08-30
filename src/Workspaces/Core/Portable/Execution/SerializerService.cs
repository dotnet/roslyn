// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
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
    internal partial class SerializerService : ISerializerService
    {
        private static readonly Func<WellKnownSynchronizationKind, string> s_logKind = k => k.ToString();

        private readonly HostWorkspaceServices _workspaceServices;

        private readonly IReferenceSerializationService _hostSerializationService;
        private readonly ITemporaryStorageService2 _tempService;
        private readonly ITextFactoryService _textService;

        private readonly ConcurrentDictionary<string, IOptionsSerializationService> _lazyLanguageSerializationService;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SerializerService(HostWorkspaceServices workspaceServices)
        {
            _workspaceServices = workspaceServices;

            _hostSerializationService = _workspaceServices.GetService<IReferenceSerializationService>();
            _tempService = _workspaceServices.GetService<ITemporaryStorageService>() as ITemporaryStorageService2;
            _textService = _workspaceServices.GetService<ITextFactoryService>();

            _lazyLanguageSerializationService = new ConcurrentDictionary<string, IOptionsSerializationService>(concurrencyLevel: 2, capacity: _workspaceServices.SupportedLanguages.Count());
        }

        public Checksum CreateChecksum(object value, CancellationToken cancellationToken)
        {
            var kind = value.GetWellKnownSynchronizationKind();

            using (Logger.LogBlock(FunctionId.Serializer_CreateChecksum, s_logKind, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (value is IChecksummedObject)
                {
                    return ((IChecksummedObject)value).Checksum;
                }

                switch (kind)
                {
                    case WellKnownSynchronizationKind.Null:
                        return Checksum.Null;

                    case WellKnownSynchronizationKind.CompilationOptions:
                    case WellKnownSynchronizationKind.ParseOptions:
                    case WellKnownSynchronizationKind.ProjectReference:
                        return Checksum.Create(kind, value, this);

                    case WellKnownSynchronizationKind.MetadataReference:
                        return Checksum.Create(kind, _hostSerializationService.CreateChecksum((MetadataReference)value, cancellationToken));

                    case WellKnownSynchronizationKind.AnalyzerReference:
                        return Checksum.Create(kind, _hostSerializationService.CreateChecksum((AnalyzerReference)value, usePathFromAssembly: true, cancellationToken));

                    case WellKnownSynchronizationKind.SourceText:
                        return Checksum.Create(kind, ((SourceText)value).GetChecksum());

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

            using (Logger.LogBlock(FunctionId.Serializer_Serialize, s_logKind, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (value is ChecksumWithChildren)
                {
                    SerializeChecksumWithChildren((ChecksumWithChildren)value, writer, cancellationToken);
                    return;
                }

                switch (kind)
                {
                    case WellKnownSynchronizationKind.Null:
                        // do nothing
                        return;

                    case WellKnownSynchronizationKind.SolutionAttributes:
                    case WellKnownSynchronizationKind.ProjectAttributes:
                    case WellKnownSynchronizationKind.DocumentAttributes:
                        ((IObjectWritable)value).WriteTo(writer);
                        return;

                    case WellKnownSynchronizationKind.CompilationOptions:
                        SerializeCompilationOptions((CompilationOptions)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.ParseOptions:
                        SerializeParseOptions((ParseOptions)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.ProjectReference:
                        SerializeProjectReference((ProjectReference)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.MetadataReference:
                        SerializeMetadataReference((MetadataReference)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.AnalyzerReference:
                        SerializeAnalyzerReference((AnalyzerReference)value, writer, usePathFromAssembly: true, cancellationToken: cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.SourceText:
                        SerializeSourceText(storage: null, text: (SourceText)value, writer: writer, cancellationToken: cancellationToken);
                        return;

                    default:
                        // object that is not part of solution is not supported since we don't know what inputs are required to
                        // serialize it
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        public T Deserialize<T>(WellKnownSynchronizationKind kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Serializer_Deserialize, s_logKind, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (kind)
                {
                    case WellKnownSynchronizationKind.Null:
                        return default;

                    case WellKnownSynchronizationKind.SolutionState:
                    case WellKnownSynchronizationKind.ProjectState:
                    case WellKnownSynchronizationKind.DocumentState:
                    case WellKnownSynchronizationKind.Projects:
                    case WellKnownSynchronizationKind.Documents:
                    case WellKnownSynchronizationKind.TextDocuments:
                    case WellKnownSynchronizationKind.AnalyzerConfigDocuments:
                    case WellKnownSynchronizationKind.ProjectReferences:
                    case WellKnownSynchronizationKind.MetadataReferences:
                    case WellKnownSynchronizationKind.AnalyzerReferences:
                        return (T)(object)DeserializeChecksumWithChildren(reader, cancellationToken);

                    case WellKnownSynchronizationKind.SolutionAttributes:
                        return (T)(object)SolutionInfo.SolutionAttributes.ReadFrom(reader);
                    case WellKnownSynchronizationKind.ProjectAttributes:
                        return (T)(object)ProjectInfo.ProjectAttributes.ReadFrom(reader);
                    case WellKnownSynchronizationKind.DocumentAttributes:
                        return (T)(object)DocumentInfo.DocumentAttributes.ReadFrom(reader);
                    case WellKnownSynchronizationKind.CompilationOptions:
                        return (T)(object)DeserializeCompilationOptions(reader, cancellationToken);
                    case WellKnownSynchronizationKind.ParseOptions:
                        return (T)(object)DeserializeParseOptions(reader, cancellationToken);
                    case WellKnownSynchronizationKind.ProjectReference:
                        return (T)(object)DeserializeProjectReference(reader, cancellationToken);
                    case WellKnownSynchronizationKind.MetadataReference:
                        return (T)(object)DeserializeMetadataReference(reader, cancellationToken);
                    case WellKnownSynchronizationKind.AnalyzerReference:
                        return (T)(object)DeserializeAnalyzerReference(reader, cancellationToken);
                    case WellKnownSynchronizationKind.SourceText:
                        return (T)(object)DeserializeSourceText(reader, cancellationToken);
                    case WellKnownSynchronizationKind.OptionSet:
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
