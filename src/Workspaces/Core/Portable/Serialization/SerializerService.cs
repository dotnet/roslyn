// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal partial class SerializerService : ISerializerService
    {
        [ExportWorkspaceServiceFactory(typeof(ISerializerService), layer: ServiceLayer.Default), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new SerializerService(workspaceServices);
        }

        private static readonly Func<WellKnownSynchronizationKind, string> s_logKind = k => k.ToString();

        private readonly HostWorkspaceServices _workspaceServices;

        private readonly ITemporaryStorageService _storageService;
        private readonly ITextFactoryService _textService;
        private readonly IDocumentationProviderService? _documentationService;
        private readonly IAnalyzerAssemblyLoaderProvider _analyzerLoaderProvider;

        private readonly ConcurrentDictionary<string, IOptionsSerializationService> _lazyLanguageSerializationService;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        private SerializerService(HostWorkspaceServices workspaceServices)
        {
            _workspaceServices = workspaceServices;

            _storageService = workspaceServices.GetRequiredService<ITemporaryStorageService>();
            _textService = workspaceServices.GetRequiredService<ITextFactoryService>();
            _analyzerLoaderProvider = workspaceServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
            _documentationService = workspaceServices.GetService<IDocumentationProviderService>();

            _lazyLanguageSerializationService = new ConcurrentDictionary<string, IOptionsSerializationService>(concurrencyLevel: 2, capacity: _workspaceServices.SupportedLanguages.Count());
        }

        public Checksum CreateChecksum(object value, CancellationToken cancellationToken)
        {
            var kind = value.GetWellKnownSynchronizationKind();

            using (Logger.LogBlock(FunctionId.Serializer_CreateChecksum, s_logKind, kind, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (value is IChecksummedObject checksummedObject)
                {
                    return checksummedObject.Checksum;
                }

                switch (kind)
                {
                    case WellKnownSynchronizationKind.Null:
                        return Checksum.Null;

                    case WellKnownSynchronizationKind.CompilationOptions:
                    case WellKnownSynchronizationKind.ParseOptions:
                    case WellKnownSynchronizationKind.ProjectReference:
                    case WellKnownSynchronizationKind.OptionSet:
                        return Checksum.Create(kind, value, this);

                    case WellKnownSynchronizationKind.MetadataReference:
                        return Checksum.Create(kind, CreateChecksum((MetadataReference)value, cancellationToken));

                    case WellKnownSynchronizationKind.AnalyzerReference:
                        return Checksum.Create(kind, CreateChecksum((AnalyzerReference)value, cancellationToken));

                    case WellKnownSynchronizationKind.SerializableSourceText:
                        return Checksum.Create(kind, ((SerializableSourceText)value).GetChecksum());

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

                if (value is ChecksumWithChildren checksumWithChildren)
                {
                    SerializeChecksumWithChildren(checksumWithChildren, writer, cancellationToken);
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
                        SerializeAnalyzerReference((AnalyzerReference)value, writer, cancellationToken: cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.SerializableSourceText:
                        SerializeSourceText((SerializableSourceText)value, writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.SourceText:
                        SerializeSourceText(new SerializableSourceText((SourceText)value), writer, cancellationToken);
                        return;

                    case WellKnownSynchronizationKind.OptionSet:
                        SerializeOptionSet((SerializableOptionSet)value, writer, cancellationToken);
                        return;

                    default:
                        // object that is not part of solution is not supported since we don't know what inputs are required to
                        // serialize it
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        public T? Deserialize<T>(WellKnownSynchronizationKind kind, ObjectReader reader, CancellationToken cancellationToken)
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
                    case WellKnownSynchronizationKind.SerializableSourceText:
                        return (T)(object)DeserializeSerializableSourceText(reader, cancellationToken);
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
            => _lazyLanguageSerializationService.GetOrAdd(languageName, n => _workspaceServices.GetLanguageServices(n).GetRequiredService<IOptionsSerializationService>());
    }

    // TODO: convert this to sub class rather than using enum with if statement.
    internal enum SerializationKinds
    {
        Bits,
        FilePath,
        MemoryMapFile
    }
}
