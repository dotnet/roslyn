// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

#if NETCOREAPP
[SupportedOSPlatform("windows")]
#endif
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

    private readonly TemporaryStorageService _storageService;
    private readonly ITextFactoryService _textService;
    private readonly IDocumentationProviderService? _documentationService;
    private readonly IAnalyzerAssemblyLoaderProvider _analyzerLoaderProvider;

    private readonly ConcurrentDictionary<string, IOptionsSerializationService> _lazyLanguageSerializationService;

    [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    private protected SerializerService(SolutionServices workspaceServices)
    {
        _workspaceServices = workspaceServices;

        // Serialization is only involved when we have a remote process.  Which is only in VS. So the type of the
        // storage service here is well known.
        _storageService = (TemporaryStorageService)workspaceServices.GetRequiredService<ITemporaryStorageServiceInternal>();
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
                case WellKnownSynchronizationKind.FallbackAnalyzerOptions:
                    return Checksum.Create(value, this, cancellationToken);

                case WellKnownSynchronizationKind.MetadataReference:
                    return CreateChecksum((MetadataReference)value, cancellationToken);

                case WellKnownSynchronizationKind.AnalyzerReference:
                    return CreateChecksum((AnalyzerReference)value, cancellationToken);

                case WellKnownSynchronizationKind.SerializableSourceText:
                    throw new InvalidOperationException("Clients can already get a checksum directly from a SerializableSourceText");

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
                    SerializeMetadataReference((MetadataReference)value, writer, cancellationToken);
                    return;

                case WellKnownSynchronizationKind.AnalyzerReference:
                    SerializeAnalyzerReference((AnalyzerReference)value, writer, cancellationToken: cancellationToken);
                    return;

                case WellKnownSynchronizationKind.SerializableSourceText:
                    SerializeSourceText((SerializableSourceText)value, writer, cancellationToken);
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

                case WellKnownSynchronizationKind.SourceGeneratorExecutionVersionMap:
                    ((SourceGeneratorExecutionVersionMap)value).WriteTo(writer);
                    return;

                case WellKnownSynchronizationKind.FallbackAnalyzerOptions:
                    Write(writer, (ImmutableDictionary<string, StructuredAnalyzerConfigOptions>)value);
                    return;

                default:
                    // object that is not part of solution is not supported since we don't know what inputs are required to
                    // serialize it
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }

    private static void Write(ObjectWriter writer, ImmutableDictionary<string, StructuredAnalyzerConfigOptions> optionsByLanguage)
    {
        // Only serialize options for C#/VB since other languages are not OOP.

        var csOptions = ImmutableDictionary.GetValueOrDefault(optionsByLanguage, LanguageNames.CSharp);
        var vbOptions = ImmutableDictionary.GetValueOrDefault(optionsByLanguage, LanguageNames.VisualBasic);

        writer.WriteCompressedUInt((uint)((csOptions != null ? 1 : 0) + (vbOptions != null ? 1 : 0)));

        Serialize(LanguageNames.CSharp, csOptions);
        Serialize(LanguageNames.VisualBasic, vbOptions);

        void Serialize(string language, StructuredAnalyzerConfigOptions? options)
        {
            if (options != null)
            {
                writer.WriteString(language);

                // order for deterministic checksums
                foreach (var key in options.Keys.Order())
                {
                    if (options.TryGetValue(key, out var value))
                    {
                        writer.WriteString(key);
                        writer.WriteString(value);
                    }
                }

                // terminator:
                writer.WriteString(null);
            }
        }
    }

    private static ImmutableDictionary<string, StructuredAnalyzerConfigOptions> ReadFallbackAnalyzerOptions(ObjectReader reader)
    {
        var count = reader.ReadCompressedUInt();
        if (count == 0)
        {
            return ImmutableDictionary<string, StructuredAnalyzerConfigOptions>.Empty;
        }

        // We only serialize C# and VB options (if present):
        Contract.ThrowIfFalse(count <= 2);

        var optionsByLanguage = ImmutableDictionary.CreateBuilder<string, StructuredAnalyzerConfigOptions>();
        var options = ImmutableDictionary.CreateBuilder<string, string>();

        for (var i = 0; i < count; i++)
        {
            var language = reader.ReadRequiredString();
            Contract.ThrowIfFalse(language is LanguageNames.CSharp or LanguageNames.VisualBasic);

            while (true)
            {
                var key = reader.ReadString();
                if (key == null)
                {
                    break;
                }

                var value = reader.ReadRequiredString();
                options.Add(key, value);
            }

            optionsByLanguage.Add(language, StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(options.ToImmutable())));
            options.Clear();
        }

        return optionsByLanguage.ToImmutable();
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
                WellKnownSynchronizationKind.SourceGeneratorExecutionVersionMap => SourceGeneratorExecutionVersionMap.Deserialize(reader),
                WellKnownSynchronizationKind.FallbackAnalyzerOptions => ReadFallbackAnalyzerOptions(reader),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }

    private IOptionsSerializationService GetOptionsSerializationService(string languageName)
        => _lazyLanguageSerializationService.GetOrAdd(languageName, n => _workspaceServices.GetLanguageServices(n).GetRequiredService<IOptionsSerializationService>());

    public Checksum CreateParseOptionsChecksum(ParseOptions value)
        => Checksum.Create((value, @this: this), static (tuple, writer) => tuple.@this.SerializeParseOptions(tuple.value, writer));
}

// TODO: convert this to sub class rather than using enum with if statement.
internal enum SerializationKinds
{
    Bits,
    MemoryMapFile
}
