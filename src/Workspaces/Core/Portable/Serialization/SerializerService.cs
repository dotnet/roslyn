// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

#if NET
[SupportedOSPlatform("windows")]
#endif
[method: Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
internal partial class SerializerService(SolutionServices workspaceServices) : ISerializerService
{
    [ExportWorkspaceServiceFactory(typeof(ISerializerService), layer: ServiceLayer.Default), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class Factory() : IWorkspaceServiceFactory
    {
        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SerializerService(workspaceServices.SolutionServices);
    }

    private static readonly Func<WellKnownSynchronizationKind, string> s_logKind = k => k.ToString();

    // Serialization to temporary storage is only involved when we have a remote process.  Which is only in VS. So the
    // type of the storage service here is well known.  However the serializer is created in other cases (e.g. to
    // compute project state checksums). So lazily instantiate the storage service to avoid attempting to get the
    // TemporaryStorageService when not available.

    private readonly Lazy<TemporaryStorageService> _storageService = new(() => (TemporaryStorageService)workspaceServices.GetRequiredService<ITemporaryStorageServiceInternal>());
    private readonly ITextFactoryService _textService = workspaceServices.GetRequiredService<ITextFactoryService>();
    private readonly IDocumentationProviderService? _documentationService = workspaceServices.GetService<IDocumentationProviderService>();
    private readonly IAnalyzerAssemblyLoaderProvider _analyzerLoaderProvider = workspaceServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

    private readonly ConcurrentDictionary<string, IOptionsSerializationService> _lazyLanguageSerializationService = new(concurrencyLevel: 2, capacity: workspaceServices.SupportedLanguages.Count());

    public Checksum CreateChecksum(object value, CancellationToken cancellationToken)
    {
        var kind = value.GetWellKnownSynchronizationKind();

        using (Logger.LogBlock(FunctionId.Serializer_CreateChecksum, s_logKind, kind, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (kind)
            {
                case WellKnownSynchronizationKind.CompilationOptions:
                case WellKnownSynchronizationKind.ParseOptions:
                case WellKnownSynchronizationKind.ProjectReference:
                case WellKnownSynchronizationKind.SourceGeneratedDocumentIdentity:
                case WellKnownSynchronizationKind.FallbackAnalyzerOptions:
                    return Checksum.Create(value, this, cancellationToken);

                case WellKnownSynchronizationKind.MetadataReference:
                    return CreateChecksum((MetadataReference)value);

                case WellKnownSynchronizationKind.AnalyzerReference:
                    return CreateChecksum((AnalyzerReference)value);

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
                    SerializeProjectReference((ProjectReference)value, writer);
                    return;

                case WellKnownSynchronizationKind.MetadataReference:
                    SerializeMetadataReference((MetadataReference)value, writer);
                    return;

                case WellKnownSynchronizationKind.AnalyzerReference:
                    SerializeAnalyzerReference((AnalyzerReference)value, writer);
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
                WellKnownSynchronizationKind.MetadataReference => DeserializeMetadataReference(reader),
                WellKnownSynchronizationKind.AnalyzerReference => DeserializeAnalyzerReference(reader),
                WellKnownSynchronizationKind.SerializableSourceText => SerializableSourceText.Deserialize(reader, _storageService.Value, _textService, cancellationToken),
                WellKnownSynchronizationKind.SourceGeneratorExecutionVersionMap => SourceGeneratorExecutionVersionMap.Deserialize(reader),
                WellKnownSynchronizationKind.FallbackAnalyzerOptions => ReadFallbackAnalyzerOptions(reader),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }

    private IOptionsSerializationService GetOptionsSerializationService(string languageName)
        => _lazyLanguageSerializationService.GetOrAdd(languageName, n => workspaceServices.GetLanguageServices(n).GetRequiredService<IOptionsSerializationService>());

    public Checksum CreateParseOptionsChecksum(ParseOptions value)
        => Checksum.Create((value, @this: this), static (tuple, writer) => tuple.@this.SerializeParseOptions(tuple.value, writer));
}

// TODO: convert this to sub class rather than using enum with if statement.
internal enum SerializationKinds
{
    Bits,
    MemoryMapFile
}
