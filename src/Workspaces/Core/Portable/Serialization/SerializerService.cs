// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

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
            => new SerializerService(workspaceServices.SolutionServices);
    }

    private static readonly Func<WellKnownSynchronizationKind, string> s_logKind = k => k.ToString();

    private readonly SolutionServices _workspaceServices;

    private readonly ITemporaryStorageServiceInternal _storageService;
    private readonly ITextFactoryService _textService;
    private readonly IDocumentationProviderService? _documentationService;
    private readonly IAnalyzerAssemblyLoaderProvider _analyzerLoaderProvider;

    private readonly ConcurrentDictionary<string, IOptionsSerializationService> _lazyLanguageSerializationService;

    [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    private protected SerializerService(SolutionServices workspaceServices)
    {
        _workspaceServices = workspaceServices;

        _storageService = workspaceServices.GetRequiredService<ITemporaryStorageServiceInternal>();
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
                case WellKnownSynchronizationKind.CompilationOptions:
                case WellKnownSynchronizationKind.ParseOptions:
                case WellKnownSynchronizationKind.ProjectReference:
                case WellKnownSynchronizationKind.SourceGeneratedDocumentIdentity:
                    return Checksum.Create(value, this);

                case WellKnownSynchronizationKind.MetadataReference:
                    return CreateChecksum((MetadataReference)value, cancellationToken);

                case WellKnownSynchronizationKind.AnalyzerReference:
                    return CreateChecksum((AnalyzerReference)value, cancellationToken);

                case WellKnownSynchronizationKind.SerializableSourceText:
                    return Checksum.Create(((SerializableSourceText)value).GetContentHash());

                case WellKnownSynchronizationKind.SourceText:
                    return Checksum.Create(((SourceText)value).GetContentHash());

                default:
                    // object that is not part of solution is not supported since we don't know what inputs are required to
                    // serialize it
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }

    public void Serialize(object value, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
    {
        var kind = value.GetWellKnownSynchronizationKind();

        using (Logger.LogBlock(FunctionId.Serializer_Serialize, s_logKind, kind, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (kind)
            {
                case WellKnownSynchronizationKind.SolutionAttributes:
                    ((SolutionInfo.SolutionAttributes)value).WriteTo(writer);
                    return;

                case WellKnownSynchronizationKind.ProjectAttributes:
                    ((ProjectInfo.ProjectAttributes)value).WriteTo(writer);
                    return;

                case WellKnownSynchronizationKind.DocumentAttributes:
                    ((DocumentInfo.DocumentAttributes)value).WriteTo(writer);
                    return;

                case WellKnownSynchronizationKind.SourceGeneratedDocumentIdentity:
                    ((SourceGeneratedDocumentIdentity)value).WriteTo(writer);
                    return;

                case WellKnownSynchronizationKind.CompilationOptions:
                    SerializeCompilationOptions((CompilationOptions)value, writer, cancellationToken);
                    return;

                case WellKnownSynchronizationKind.ParseOptions:
                    cancellationToken.ThrowIfCancellationRequested();
                    SerializeParseOptions((ParseOptions)value, writer);
                    return;

                case WellKnownSynchronizationKind.ProjectReference:
                    SerializeProjectReference((ProjectReference)value, writer, cancellationToken);
                    return;

                case WellKnownSynchronizationKind.MetadataReference:
                    SerializeMetadataReference((MetadataReference)value, writer, context, cancellationToken);
                    return;

                case WellKnownSynchronizationKind.AnalyzerReference:
                    SerializeAnalyzerReference((AnalyzerReference)value, writer, cancellationToken: cancellationToken);
                    return;

                case WellKnownSynchronizationKind.SerializableSourceText:
                    SerializeSourceText((SerializableSourceText)value, writer, context, cancellationToken);
                    return;

                case WellKnownSynchronizationKind.SourceText:
                    SerializeSourceText(new SerializableSourceText((SourceText)value), writer, context, cancellationToken);
                    return;

                case WellKnownSynchronizationKind.SolutionCompilationState:
                    ((SolutionCompilationStateChecksums)value).Serialize(writer);
                    return;

                case WellKnownSynchronizationKind.SolutionState:
                    ((SolutionStateChecksums)value).Serialize(writer);
                    return;

                case WellKnownSynchronizationKind.ProjectState:
                    ((ProjectStateChecksums)value).Serialize(writer);
                    return;

                case WellKnownSynchronizationKind.DocumentState:
                    ((DocumentStateChecksums)value).Serialize(writer);
                    return;

                case WellKnownSynchronizationKind.ChecksumCollection:
                    ((ChecksumCollection)value).WriteTo(writer);
                    return;

                default:
                    // object that is not part of solution is not supported since we don't know what inputs are required to
                    // serialize it
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }

    public object Deserialize(WellKnownSynchronizationKind kind, ObjectReader reader, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Serializer_Deserialize, s_logKind, kind, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            return kind switch
            {
                WellKnownSynchronizationKind.SolutionCompilationState => SolutionCompilationStateChecksums.Deserialize(reader),
                WellKnownSynchronizationKind.SolutionState => SolutionStateChecksums.Deserialize(reader),
                WellKnownSynchronizationKind.ProjectState => ProjectStateChecksums.Deserialize(reader),
                WellKnownSynchronizationKind.DocumentState => DocumentStateChecksums.Deserialize(reader),
                WellKnownSynchronizationKind.ChecksumCollection => ChecksumCollection.ReadFrom(reader),
                WellKnownSynchronizationKind.SolutionAttributes => SolutionInfo.SolutionAttributes.ReadFrom(reader),
                WellKnownSynchronizationKind.ProjectAttributes => ProjectInfo.ProjectAttributes.ReadFrom(reader),
                WellKnownSynchronizationKind.DocumentAttributes => DocumentInfo.DocumentAttributes.ReadFrom(reader),
                WellKnownSynchronizationKind.SourceGeneratedDocumentIdentity => SourceGeneratedDocumentIdentity.ReadFrom(reader),
                WellKnownSynchronizationKind.CompilationOptions => DeserializeCompilationOptions(reader, cancellationToken),
                WellKnownSynchronizationKind.ParseOptions => DeserializeParseOptions(reader, cancellationToken),
                WellKnownSynchronizationKind.ProjectReference => DeserializeProjectReference(reader, cancellationToken),
                WellKnownSynchronizationKind.MetadataReference => DeserializeMetadataReference(reader, cancellationToken),
                WellKnownSynchronizationKind.AnalyzerReference => DeserializeAnalyzerReference(reader, cancellationToken),
                WellKnownSynchronizationKind.SerializableSourceText => SerializableSourceText.Deserialize(reader, _storageService, _textService, cancellationToken),
                WellKnownSynchronizationKind.SourceText => DeserializeSourceText(reader, cancellationToken),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }

    private IOptionsSerializationService GetOptionsSerializationService(string languageName)
        => _lazyLanguageSerializationService.GetOrAdd(languageName, n => _workspaceServices.GetLanguageServices(n).GetRequiredService<IOptionsSerializationService>());

    public Checksum CreateParseOptionsChecksum(ParseOptions value)
        => Checksum.Create(value, this);
}

// TODO: convert this to sub class rather than using enum with if statement.
internal enum SerializationKinds
{
    Bits,
    MemoryMapFile
}
