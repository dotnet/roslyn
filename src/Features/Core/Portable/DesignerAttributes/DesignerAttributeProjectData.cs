// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal class DesignerAttributeProjectData
    {
        private const string StreamName = "<DesignerAttribute>";
        private const string FormatVersion = "4";

        public readonly VersionStamp SemanticVersion;
        public readonly ImmutableDictionary<string, DesignerAttributeDocumentData> PathToDocumentData;

        public DesignerAttributeProjectData(
            VersionStamp semanticVersion, ImmutableDictionary<string, DesignerAttributeDocumentData> pathToDocumentData)
        {
            SemanticVersion = semanticVersion;
            PathToDocumentData = pathToDocumentData;
        }

        public static async Task<DesignerAttributeProjectData> ReadAsync(
            Project project, CancellationToken cancellationToken)
        {
            try
            {
                var solution = project.Solution;
                var storageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();

                using (var persistenceService = storageService.GetStorage(solution, checkBranchId: false))
                using (var stream = await persistenceService.ReadStreamAsync(project, StreamName, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream, cancellationToken))
                {
                    if (reader != null)
                    {
                        var version = reader.ReadString();
                        if (version == FormatVersion)
                        {
                            var semanticVersion = VersionStamp.ReadFrom(reader);

                            var resultCount = reader.ReadInt32();
                            var builder = ImmutableDictionary.CreateBuilder<string, DesignerAttributeDocumentData>();

                            for (var i = 0; i < resultCount; i++)
                            {
                                var filePath = reader.ReadString();
                                var attribute = reader.ReadString();

                                builder[filePath] = new DesignerAttributeDocumentData(filePath, attribute);
                            }

                            return new DesignerAttributeProjectData(semanticVersion, builder.ToImmutable());
                        }
                    }
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return null;
        }

        public async Task PersistAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                var solution = project.Solution;
                var storageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();

                using (var storage = storageService.GetStorage(solution, checkBranchId: false))
                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    writer.WriteString(FormatVersion);
                    this.SemanticVersion.WriteTo(writer);

                    writer.WriteInt32(this.PathToDocumentData.Count);

                    foreach (var kvp in this.PathToDocumentData)
                    {
                        var result = kvp.Value;
                        writer.WriteString(result.FilePath);
                        writer.WriteString(result.DesignerAttributeArgument);
                    }

                    stream.Position = 0;
                    await storage.WriteStreamAsync(project, StreamName, stream, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }
        }
    }
}
