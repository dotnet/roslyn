// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics
{
    /// <summary>
    /// DiagnosticData serializer
    /// </summary>
    internal readonly struct DiagnosticDataSerializer
    {
        // version of serialized format
        private const int FormatVersion = 1;

        // version of analyzer that produced this data
        public readonly VersionStamp AnalyzerVersion;

        // version of project this data belong to
        public readonly VersionStamp Version;

        public DiagnosticDataSerializer(VersionStamp analyzerVersion, VersionStamp version)
        {
            AnalyzerVersion = analyzerVersion;
            Version = version;
        }

        public async Task<bool> SerializeAsync(IPersistentStorageService persistentService, Project project, Document? document, string key, ImmutableArray<DiagnosticData> items, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document == null || document.Project == project);

            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new ObjectWriter(stream, cancellationToken: cancellationToken);

            WriteDiagnosticData(writer, items, cancellationToken);

            using var storage = persistentService.GetStorage(project.Solution);

            stream.Position = 0;

            var writeTask = (document != null) ?
                storage.WriteStreamAsync(document, key, stream, cancellationToken) :
                storage.WriteStreamAsync(project, key, stream, cancellationToken);

            return await writeTask.ConfigureAwait(false);
        }

        public async ValueTask<ImmutableArray<DiagnosticData>> DeserializeAsync(IPersistentStorageService persistentService, Project project, Document? document, string key, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document == null || document.Project == project);

            using var storage = persistentService.GetStorage(project.Solution);

            var readTask = (document != null) ?
                storage.ReadStreamAsync(document, key, cancellationToken) :
                storage.ReadStreamAsync(project, key, cancellationToken);

            using var stream = await readTask.ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream);

            if (reader == null)
            {
                return default;
            }

            return ReadDiagnosticData(reader, project, document, cancellationToken);
        }

        public void WriteDiagnosticData(ObjectWriter writer, ImmutableArray<DiagnosticData> items, CancellationToken cancellationToken)
        {
            writer.WriteInt32(FormatVersion);

            AnalyzerVersion.WriteTo(writer);
            Version.WriteTo(writer);

            writer.WriteInt32(items.Length);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                writer.WriteString(item.Id);
                writer.WriteString(item.Category);

                writer.WriteString(item.Message);
                writer.WriteString(item.ENUMessageForBingSearch);
                writer.WriteString(item.Title);
                writer.WriteString(item.Description);
                writer.WriteString(item.HelpLink);
                writer.WriteInt32((int)item.Severity);
                writer.WriteInt32((int)item.DefaultSeverity);
                writer.WriteBoolean(item.IsEnabledByDefault);
                writer.WriteBoolean(item.IsSuppressed);
                writer.WriteInt32(item.WarningLevel);

                // unused
                writer.WriteInt32(0);
                writer.WriteInt32(0);

                WriteLocation(writer, item.DataLocation);
                WriteAdditionalLocations(writer, item.AdditionalLocations, cancellationToken);

                writer.WriteInt32(item.CustomTags.Count);
                foreach (var tag in item.CustomTags)
                {
                    writer.WriteString(tag);
                }

                writer.WriteInt32(item.Properties.Count);
                foreach (var property in item.Properties)
                {
                    writer.WriteString(property.Key);
                    writer.WriteString(property.Value);
                }
            }
        }

        private static void WriteAdditionalLocations(ObjectWriter writer, IReadOnlyCollection<DiagnosticDataLocation> additionalLocations, CancellationToken cancellationToken)
        {
            writer.WriteInt32(additionalLocations.Count);

            foreach (var location in additionalLocations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteLocation(writer, location);
            }
        }

        private static void WriteLocation(ObjectWriter writer, DiagnosticDataLocation? item)
        {
            if (item == null)
            {
                writer.WriteBoolean(false);
                return;
            }

            writer.WriteBoolean(true);

            if (item.SourceSpan.HasValue)
            {
                writer.WriteBoolean(true);
                writer.WriteInt32(item.SourceSpan.Value.Start);
                writer.WriteInt32(item.SourceSpan.Value.Length);
            }
            else
            {
                writer.WriteBoolean(false);
            }

            writer.WriteString(item.OriginalFilePath);
            writer.WriteInt32(item.OriginalStartLine);
            writer.WriteInt32(item.OriginalStartColumn);
            writer.WriteInt32(item.OriginalEndLine);
            writer.WriteInt32(item.OriginalEndColumn);

            writer.WriteString(item.MappedFilePath);
            writer.WriteInt32(item.MappedStartLine);
            writer.WriteInt32(item.MappedStartColumn);
            writer.WriteInt32(item.MappedEndLine);
            writer.WriteInt32(item.MappedEndColumn);
        }

        public ImmutableArray<DiagnosticData> ReadDiagnosticData(ObjectReader reader, Project project, Document? document, CancellationToken cancellationToken)
        {
            try
            {
                var format = reader.ReadInt32();
                if (format != FormatVersion)
                {
                    return default;
                }

                // saved data is for same analyzer of different version of dll
                var analyzerVersion = VersionStamp.ReadFrom(reader);
                if (analyzerVersion != AnalyzerVersion)
                {
                    return default;
                }

                var version = VersionStamp.ReadFrom(reader);
                if (version != VersionStamp.Default && version != Version)
                {
                    return default;
                }

                return ReadDiagnosticDataArray(reader, project, document, cancellationToken);
            }
            catch (Exception)
            {
                return default;
            }
        }

        private static ImmutableArray<DiagnosticData> ReadDiagnosticDataArray(ObjectReader reader, Project project, Document? document, CancellationToken cancellationToken)
        {
            var count = reader.ReadInt32();
            if (count == 0)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            var builder = ArrayBuilder<DiagnosticData>.GetInstance(count);

            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var id = reader.ReadString();
                var category = reader.ReadString();

                var message = reader.ReadString();
                var messageFormat = reader.ReadString();
                var title = reader.ReadString();
                var description = reader.ReadString();
                var helpLink = reader.ReadString();
                var severity = (DiagnosticSeverity)reader.ReadInt32();
                var defaultSeverity = (DiagnosticSeverity)reader.ReadInt32();
                var isEnabledByDefault = reader.ReadBoolean();
                var isSuppressed = reader.ReadBoolean();
                var warningLevel = reader.ReadInt32();

                // these fields are unused - the actual span is read in ReadLocation
                _ = reader.ReadInt32();
                _ = reader.ReadInt32();

                var location = ReadLocation(project, reader, document);
                var additionalLocations = ReadAdditionalLocations(project, reader);

                var customTagsCount = reader.ReadInt32();
                var customTags = GetCustomTags(reader, customTagsCount);

                var propertiesCount = reader.ReadInt32();
                var properties = GetProperties(reader, propertiesCount);

                builder.Add(new DiagnosticData(
                    id: id,
                    category: category,
                    message: message,
                    enuMessageForBingSearch: messageFormat,
                    severity: severity,
                    defaultSeverity: defaultSeverity,
                    isEnabledByDefault: isEnabledByDefault,
                    warningLevel: warningLevel,
                    customTags: customTags,
                    properties: properties,
                    projectId: project.Id,
                    location: location,
                    additionalLocations: additionalLocations,
                    language: project.Language,
                    title: title,
                    description: description,
                    helpLink: helpLink,
                    isSuppressed: isSuppressed));
            }

            return builder.ToImmutableAndFree();
        }

        private static DiagnosticDataLocation? ReadLocation(Project project, ObjectReader reader, Document? document)
        {
            var exists = reader.ReadBoolean();
            if (!exists)
            {
                return null;
            }

            TextSpan? sourceSpan = null;
            if (reader.ReadBoolean())
            {
                sourceSpan = new TextSpan(reader.ReadInt32(), reader.ReadInt32());
            }

            var originalFile = reader.ReadString();
            var originalStartLine = reader.ReadInt32();
            var originalStartColumn = reader.ReadInt32();
            var originalEndLine = reader.ReadInt32();
            var originalEndColumn = reader.ReadInt32();

            var mappedFile = reader.ReadString();
            var mappedStartLine = reader.ReadInt32();
            var mappedStartColumn = reader.ReadInt32();
            var mappedEndLine = reader.ReadInt32();
            var mappedEndColumn = reader.ReadInt32();

            var documentId = document != null
                ? document.Id
                : project.Documents.FirstOrDefault(d => d.FilePath == originalFile)?.Id;

            return new DiagnosticDataLocation(documentId, sourceSpan,
                originalFile, originalStartLine, originalStartColumn, originalEndLine, originalEndColumn,
                mappedFile, mappedStartLine, mappedStartColumn, mappedEndLine, mappedEndColumn);
        }

        private static IReadOnlyCollection<DiagnosticDataLocation> ReadAdditionalLocations(Project project, ObjectReader reader)
        {
            var count = reader.ReadInt32();
            var result = new List<DiagnosticDataLocation>();
            for (var i = 0; i < count; i++)
            {
                var location = ReadLocation(project, reader, document: null);
                if (location != null)
                {
                    result.Add(location);
                }
            }

            return result;
        }

        private static ImmutableDictionary<string, string> GetProperties(ObjectReader reader, int count)
        {
            if (count > 0)
            {
                var properties = ImmutableDictionary.CreateBuilder<string, string>();
                for (var i = 0; i < count; i++)
                {
                    properties.Add(reader.ReadString(), reader.ReadString());
                }

                return properties.ToImmutable();
            }

            return ImmutableDictionary<string, string>.Empty;
        }

        private static IReadOnlyList<string> GetCustomTags(ObjectReader reader, int count)
        {
            if (count > 0)
            {
                var tags = new List<string>(count);
                for (var i = 0; i < count; i++)
                {
                    tags.Add(reader.ReadString());
                }

                return new ReadOnlyCollection<string>(tags);
            }

            return SpecializedCollections.EmptyReadOnlyList<string>();
        }
    }
}
