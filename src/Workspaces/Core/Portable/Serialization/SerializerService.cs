// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public async ValueTask SerializeAsync(object value, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
    {
        var kind = value.GetWellKnownSynchronizationKind();

        using (Logger.LogBlock(FunctionId.Serializer_Serialize, s_logKind, kind, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (kind)
            {
                case WellKnownSynchronizationKind.SolutionAttributes:
                    await ((SolutionInfo.SolutionAttributes)value).WriteToAsync(writer).ConfigureAwait(false);
                    return;

                case WellKnownSynchronizationKind.ProjectAttributes:
                    await ((ProjectInfo.ProjectAttributes)value).WriteToAsync(writer).ConfigureAwait(false);
                    return;

                case WellKnownSynchronizationKind.DocumentAttributes:
                    await ((DocumentInfo.DocumentAttributes)value).WriteToAsync(writer).ConfigureAwait(false);
                    return;

                case WellKnownSynchronizationKind.SourceGeneratedDocumentIdentity:
                    await ((SourceGeneratedDocumentIdentity)value).WriteToAsync(writer).ConfigureAwait(false);
                    return;

                case WellKnownSynchronizationKind.CompilationOptions:
                    await SerializeCompilationOptionsAsync((CompilationOptions)value, writer, cancellationToken).ConfigureAwait(false);
                    return;

                case WellKnownSynchronizationKind.ParseOptions:
                    await SerializeParseOptionsAsync((ParseOptions)value, writer).ConfigureAwait(false);
                    return;

                case WellKnownSynchronizationKind.ProjectReference:
                    await SerializeProjectReferenceAsync((ProjectReference)value, writer, cancellationToken).ConfigureAwait(false);
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
                    await ((DocumentStateChecksums)value).SerializeAsync(writer).ConfigureAwait(false);
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

    public async ValueTask<T> DeserializeAsync<T>(WellKnownSynchronizationKind kind, ObjectReader reader, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Serializer_Deserialize, s_logKind, kind, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (kind)
            {
                case WellKnownSynchronizationKind.SolutionCompilationState:
                    return (T)(object)SolutionCompilationStateChecksums.Deserialize(reader);

                case WellKnownSynchronizationKind.SolutionState:
                    return (T)(object)SolutionStateChecksums.Deserialize(reader);

                case WellKnownSynchronizationKind.ProjectState:
                    return (T)(object)ProjectStateChecksums.Deserialize(reader);

                case WellKnownSynchronizationKind.DocumentState:
                    return (T)(object)await DocumentStateChecksums.DeserializeAsync(reader).ConfigureAwait(false);

                case WellKnownSynchronizationKind.ChecksumCollection:
                    return (T)(object)ChecksumCollection.ReadFrom(reader);

                case WellKnownSynchronizationKind.SolutionAttributes:
                    return (T)(object)await SolutionInfo.SolutionAttributes.ReadFromAsync(reader).ConfigureAwait(false);
                case WellKnownSynchronizationKind.ProjectAttributes:
                    return (T)(object)await ProjectInfo.ProjectAttributes.ReadFromAsync(reader).ConfigureAwait(false);
                case WellKnownSynchronizationKind.DocumentAttributes:
                    return (T)(object)await DocumentInfo.DocumentAttributes.ReadFromAsync(reader).ConfigureAwait(false);
                case WellKnownSynchronizationKind.SourceGeneratedDocumentIdentity:
                    return (T)(object)await SourceGeneratedDocumentIdentity.ReadFromAsync(reader).ConfigureAwait(false);
                case WellKnownSynchronizationKind.CompilationOptions:
                    return (T)(object)await DeserializeCompilationOptionsAsync(reader, cancellationToken).ConfigureAwait(false);
                case WellKnownSynchronizationKind.ParseOptions:
                    return (T)(object)await DeserializeParseOptionsAsync(reader, cancellationToken).ConfigureAwait(false);
                case WellKnownSynchronizationKind.ProjectReference:
                    return (T)(object)await DeserializeProjectReferenceAsync(reader, cancellationToken).ConfigureAwait(false);
                case WellKnownSynchronizationKind.MetadataReference:
                    return (T)(object)DeserializeMetadataReference(reader, cancellationToken);
                case WellKnownSynchronizationKind.AnalyzerReference:
                    return (T)(object)DeserializeAnalyzerReference(reader, cancellationToken);
                case WellKnownSynchronizationKind.SerializableSourceText:
                    return (T)(object)await SerializableSourceText.DeserializeAsync(reader, _storageService, _textService, cancellationToken).ConfigureAwait(false);
                case WellKnownSynchronizationKind.SourceText:
                    return (T)(object)DeserializeSourceText(reader, cancellationToken);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
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
    FilePath,
    MemoryMapFile
}
