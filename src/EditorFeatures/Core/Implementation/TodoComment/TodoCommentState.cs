// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionCrawler.State;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    internal partial class TodoCommentIncrementalAnalyzer : IIncrementalAnalyzer
    {
        private class TodoCommentState : AbstractDocumentAnalyzerState<Data>
        {
            private const string FormatVersion = "1";

            protected override string StateName => "<TodoComments>";

            protected override int GetCount(Data data)
            {
                return data.Items.Length;
            }

            protected override Data TryGetExistingData(Stream stream, Document value, CancellationToken cancellationToken)
            {
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        var format = reader.ReadString();
                        if (string.Equals(format, FormatVersion))
                        {
                            var textVersion = VersionStamp.ReadFrom(reader);
                            var dataVersion = VersionStamp.ReadFrom(reader);

                            using var list = ArrayBuilder<TodoItem>.GetInstance();
                            AppendItems(reader, value, list, cancellationToken);

                            return new Data(textVersion, dataVersion, list.ToImmutable());
                        }
                    }
                }

                return null;
            }

            protected override void WriteTo(Stream stream, Data data, CancellationToken cancellationToken)
            {
                using var writer = new ObjectWriter(stream, cancellationToken: cancellationToken);

                writer.WriteString(FormatVersion);
                data.TextVersion.WriteTo(writer);
                data.SyntaxVersion.WriteTo(writer);

                writer.WriteInt32(data.Items.Length);

                foreach (var item in data.Items.OfType<TodoItem>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteInt32(item.Priority);
                    writer.WriteString(item.Message);

                    writer.WriteString(item.OriginalFilePath);
                    writer.WriteInt32(item.OriginalLine);
                    writer.WriteInt32(item.OriginalColumn);

                    writer.WriteString(item.MappedFilePath);
                    writer.WriteInt32(item.MappedLine);
                    writer.WriteInt32(item.MappedColumn);
                }
            }

            public ImmutableArray<DocumentId> GetDocumentIds()
            {
                return DataCache.Keys.ToImmutableArrayOrEmpty();
            }

            public ImmutableArray<TodoItem> GetItems_TestingOnly(DocumentId documentId)
            {
                if (this.DataCache.TryGetValue(documentId, out var entry) && entry.HasCachedData)
                {
                    return entry.Data.Items;
                }

                return ImmutableArray<TodoItem>.Empty;
            }

            private void AppendItems(ObjectReader reader, Document document, ArrayBuilder<TodoItem> list, CancellationToken cancellationToken)
            {
                var count = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var priority = reader.ReadInt32();
                    var message = reader.ReadString();

                    var originalFile = reader.ReadString();
                    var originalLine = reader.ReadInt32();
                    var originalColumn = reader.ReadInt32();

                    var mappedFile = reader.ReadString();
                    var mappedLine = reader.ReadInt32();
                    var mappedColumn = reader.ReadInt32();

                    list.Add(new TodoItem(
                        priority, message,
                        document.Id,
                        mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile));
                }
            }
        }
    }
}
