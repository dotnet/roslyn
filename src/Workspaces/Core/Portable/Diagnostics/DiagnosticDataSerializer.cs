// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
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

        public async Task<bool> SerializeAsync(IPersistentStorageService persistentService, Project project, TextDocument? textDocument, string key, ImmutableArray<DiagnosticData> items, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(textDocument == null || textDocument.Project == project);

            using var stream = SerializableBytes.CreateWritableStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                WriteDiagnosticData(writer, items, cancellationToken);
            }

            using var storage = persistentService.GetStorage(project.Solution);

            stream.Position = 0;

            var writeTask = (textDocument != null) ?
                textDocument is Document document ?
                    storage.WriteStreamAsync(document, key, stream, cancellationToken) :
                    storage.WriteStreamAsync(GetSerializationKeyForNonSourceDocument(textDocument, key), stream, cancellationToken) :
                storage.WriteStreamAsync(project, key, stream, cancellationToken);

            return await writeTask.ConfigureAwait(false);
        }

        private static string GetSerializationKeyForNonSourceDocument(TextDocument document, string key)
            => document.Id + ";" + key;

        public async ValueTask<ImmutableArray<DiagnosticData>> DeserializeAsync(IPersistentStorageService persistentService, Project project, TextDocument? textDocument, string key, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(textDocument == null || textDocument.Project == project);

            using var storage = persistentService.GetStorage(project.Solution);

            var readTask = (textDocument != null) ?
                textDocument is Document document ?
                    storage.ReadStreamAsync(document, key, cancellationToken) :
                    storage.ReadStreamAsync(GetSerializationKeyForNonSourceDocument(textDocument, key), cancellationToken) :
                storage.ReadStreamAsync(project, key, cancellationToken);

            using var stream = await readTask.ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);

            if (reader == null ||
                !TryReadDiagnosticData(reader, project, textDocument, cancellationToken, out var data))
            {
                return default;
            }

            return data;
        }

        public void WriteDiagnosticData(ObjectWriter writer, ImmutableArray<DiagnosticData> items, CancellationToken cancellationToken)
        {
            writer.WriteInt32(FormatVersion);

            AnalyzerVersion.WriteTo(writer);
            Version.WriteTo(writer);

            WriteDiagnosticDataArray(writer, items, cancellationToken);
        }

        public static void WriteDiagnosticDataNoProjectInfo(ObjectWriter writer, ImmutableArray<DiagnosticData> items, CancellationToken cancellationToken)
        {
            writer.WriteInt32(FormatVersion);
            WriteDiagnosticDataArray(writer, items, cancellationToken);
        }

        private static void WriteDiagnosticDataArray(ObjectWriter writer, ImmutableArray<DiagnosticData> items, CancellationToken cancellationToken)
        {
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

                writer.WriteInt32(item.CustomTags.Length);
                foreach (var tag in item.CustomTags)
                    writer.WriteString(tag);

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

        public bool TryReadDiagnosticData(
            ObjectReader reader,
            Project project,
            TextDocument? document,
            CancellationToken cancellationToken,
            out ImmutableArray<DiagnosticData> data)
        {
            data = default;

            try
            {
                var format = reader.ReadInt32();
                if (format != FormatVersion)
                {
                    return false;
                }

                // saved data is for same analyzer of different version of dll
                var analyzerVersion = VersionStamp.ReadFrom(reader);
                if (analyzerVersion != AnalyzerVersion)
                {
                    return false;
                }

                var version = VersionStamp.ReadFrom(reader);
                if (version != VersionStamp.Default && version != Version)
                {
                    return false;
                }

                data = ReadDiagnosticDataArray(reader, project, document, cancellationToken);
                return true;
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
            {
                return false;
            }
        }

        public static bool TryReadDiagnosticData(
            ObjectReader reader,
            DocumentKey documentKey,
            CancellationToken cancellationToken,
            out ImmutableArray<DiagnosticData> data)
        {
            data = default;

            try
            {
                var format = reader.ReadInt32();
                if (format != FormatVersion)
                {
                    return false;
                }

                data = ReadDiagnosticDataArray(reader, documentKey, cancellationToken);
                return true;
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
            {
                return false;
            }
        }

        private static void ReadDiagnosticInfo(ObjectReader reader, out DiagnosticInfo diagnosticInfo)
        {
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

            TryReadLocationInfo(reader, out var locationInfo);
            var additionalLocationInfo = ReadAdditionalLocationInfo(reader);

            var customTagsCount = reader.ReadInt32();
            var customTags = GetCustomTags(reader, customTagsCount);

            var propertiesCount = reader.ReadInt32();
            var properties = GetProperties(reader, propertiesCount);

            diagnosticInfo = new DiagnosticInfo()
            {
                Id = id,
                Category = category,
                Message = message,
                MessageFormat = messageFormat,
                Title = title,
                Description = description,
                HelpLink = helpLink,
                Severity = severity,
                DefaultSeverity = defaultSeverity,
                IsEnabledByDefault = isEnabledByDefault,
                IsSuppressed = isSuppressed,
                WarningLevel = warningLevel,
                LocationInfo = locationInfo,
                AdditionalLocationInfo = additionalLocationInfo,
                CustomTags = customTags,
                Properties = properties
            };
        }

        private static ImmutableArray<DiagnosticData> ReadDiagnosticDataArray(ObjectReader reader, Project project, TextDocument? document, CancellationToken cancellationToken)
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
                ReadDiagnosticInfo(reader, out var diagnosticInfo);

                builder.Add(diagnosticInfo.ToDiagnosticData(project, document));
            }

            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<DiagnosticData> ReadDiagnosticDataArray(ObjectReader reader, DocumentKey documentKey, CancellationToken cancellationToken)
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
                ReadDiagnosticInfo(reader, out var diagnosticInfo);

                builder.Add(diagnosticInfo.ToDiagnosticData(documentKey));
            }

            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<LocationInfo> ReadAdditionalLocationInfo(ObjectReader reader)
        {
            var count = reader.ReadInt32();
            using var _ = ArrayBuilder<LocationInfo>.GetInstance(count, out var result);
            for (var i = 0; i < count; i++)
            {
                if (TryReadLocationInfo(reader, out var locationInfo))
                {
                    result.AddIfNotNull(locationInfo);
                }
            }

            return result.ToImmutable();
        }

        private static bool TryReadLocationInfo(ObjectReader reader, out LocationInfo? locationInfo)
        {
            var exists = reader.ReadBoolean();
            if (!exists)
            {
                locationInfo = null;
                return false;
            }

            locationInfo = new LocationInfo
            {
                SourceSpan = reader.ReadBoolean() ? new TextSpan(reader.ReadInt32(), reader.ReadInt32()) : null,

                OriginalFile = reader.ReadString(),
                OriginalStartLine = reader.ReadInt32(),
                OriginalStartColumn = reader.ReadInt32(),
                OriginalEndLine = reader.ReadInt32(),
                OriginalEndColumn = reader.ReadInt32(),

                MappedFile = reader.ReadString(),
                MappedStartLine = reader.ReadInt32(),
                MappedStartColumn = reader.ReadInt32(),
                MappedEndLine = reader.ReadInt32(),
                MappedEndColumn = reader.ReadInt32()
            };

            return true;
        }

        private struct LocationInfo
        {
            public TextSpan? SourceSpan { get; set; }
            public string? OriginalFile { get; set; }
            public int OriginalStartLine { get; set; }
            public int OriginalStartColumn { get; set; }
            public int OriginalEndLine { get; set; }
            public int OriginalEndColumn { get; set; }
            public string? MappedFile { get; set; }
            public int MappedStartLine { get; set; }
            public int MappedStartColumn { get; set; }
            public int MappedEndLine { get; set; }
            public int MappedEndColumn { get; set; }

            public DiagnosticDataLocation ToDiagnosticDataLocation(Project project, TextDocument? document)
            {
                var documentId = document != null
                   ? document.Id
                   : project.Solution.GetDocumentIdsWithFilePath(OriginalFile).FirstOrDefault(documentId => documentId.ProjectId == project.Id);

                return new DiagnosticDataLocation(documentId, SourceSpan,
                      OriginalFile, OriginalStartLine, OriginalStartColumn, OriginalEndLine, OriginalEndColumn,
                      MappedFile, MappedStartLine, MappedStartColumn, MappedEndLine, MappedEndColumn);
            }

            public DiagnosticDataLocation ToDiagnosticDataLocation(DocumentKey documentKey)
            {
                var documentId = OriginalFile?.Equals(documentKey.FilePath, StringComparison.OrdinalIgnoreCase) == true
                   ? documentKey.Id
                   : null;

                return new DiagnosticDataLocation(documentId, SourceSpan,
                      OriginalFile, OriginalStartLine, OriginalStartColumn, OriginalEndLine, OriginalEndColumn,
                      MappedFile, MappedStartLine, MappedStartColumn, MappedEndLine, MappedEndColumn);
            }
        }

        private static ImmutableArray<DiagnosticDataLocation> CreateAdditionalDiagnosticLocations(Project project, ImmutableArray<LocationInfo> additionalLocationInfo)
        {
            using var _ = ArrayBuilder<DiagnosticDataLocation>.GetInstance(additionalLocationInfo.Length, out var builder);
            foreach (var info in additionalLocationInfo)
                builder.Add(info.ToDiagnosticDataLocation(project, null));

            return builder.ToImmutable();
        }

        private static ImmutableArray<DiagnosticDataLocation> CreateAdditionalDiagnosticLocations(DocumentKey documentKey, ImmutableArray<LocationInfo> additionalLocationInfo)
        {
            using var _ = ArrayBuilder<DiagnosticDataLocation>.GetInstance(additionalLocationInfo.Length, out var builder);
            foreach (var info in additionalLocationInfo)
                builder.Add(info.ToDiagnosticDataLocation(documentKey));

            return builder.ToImmutable();
        }

        private struct DiagnosticInfo
        {
            public string Id { get; set; }
            public string Category { get; set; }
            public string? Message { get; set; }
            public string? MessageFormat { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? HelpLink { get; set; }
            public DiagnosticSeverity Severity { get; set; }
            public DiagnosticSeverity DefaultSeverity { get; set; }
            public bool IsEnabledByDefault { get; set; }
            public bool IsSuppressed { get; set; }
            public int WarningLevel { get; set; }
            public LocationInfo? LocationInfo { get; set; }
            public ImmutableArray<LocationInfo> AdditionalLocationInfo { get; set; }
            public ImmutableArray<string> CustomTags { get; set; }
            public ImmutableDictionary<string, string?> Properties { get; set; }

            public DiagnosticData ToDiagnosticData(Project project, TextDocument? document)
            {
                return new DiagnosticData(
                    id: Id,
                    category: Category,
                    message: Message,
                    enuMessageForBingSearch: MessageFormat,
                    severity: Severity,
                    defaultSeverity: DefaultSeverity,
                    isEnabledByDefault: IsEnabledByDefault,
                    warningLevel: WarningLevel,
                    customTags: CustomTags,
                    properties: Properties,
                    projectId: project.Id,
                    location: LocationInfo?.ToDiagnosticDataLocation(project, document),
                    additionalLocations: CreateAdditionalDiagnosticLocations(project, AdditionalLocationInfo),
                    language: project.Language,
                    title: Title,
                    description: Description,
                    helpLink: HelpLink,
                    isSuppressed: IsSuppressed);
            }

            public DiagnosticData ToDiagnosticData(DocumentKey documentKey)
            {
                return new DiagnosticData(
                    id: Id,
                    category: Category,
                    message: Message,
                    enuMessageForBingSearch: MessageFormat,
                    severity: Severity,
                    defaultSeverity: DefaultSeverity,
                    isEnabledByDefault: IsEnabledByDefault,
                    warningLevel: WarningLevel,
                    customTags: CustomTags,
                    properties: Properties,
                    projectId: documentKey.Project.Id,
                    location: LocationInfo?.ToDiagnosticDataLocation(documentKey),
                    additionalLocations: CreateAdditionalDiagnosticLocations(documentKey, AdditionalLocationInfo),
                    language: null,
                    title: Title,
                    description: Description,
                    helpLink: HelpLink,
                    isSuppressed: IsSuppressed);
            }
        }

        private static ImmutableDictionary<string, string?> GetProperties(ObjectReader reader, int count)
        {
            if (count > 0)
            {
                var properties = ImmutableDictionary.CreateBuilder<string, string?>();
                for (var i = 0; i < count; i++)
                {
                    properties.Add(reader.ReadString(), reader.ReadString());
                }

                return properties.ToImmutable();
            }

            return ImmutableDictionary<string, string?>.Empty;
        }

        private static ImmutableArray<string> GetCustomTags(ObjectReader reader, int count)
        {
            using var _ = ArrayBuilder<string>.GetInstance(count, out var tags);
            for (var i = 0; i < count; i++)
                tags.Add(reader.ReadString());

            return tags.ToImmutable();
        }
    }
}
