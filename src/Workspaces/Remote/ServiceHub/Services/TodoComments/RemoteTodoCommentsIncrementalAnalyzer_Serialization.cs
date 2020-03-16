// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.TodoComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteTodoCommentsIncrementalAnalyzer
    {
        private const string SerializationFormat = "1";

        private async Task<PersistedTodoCommentInfo?> TryReadExistingCommentInfoAsync(
            IPersistentStorage storage, Document document, CancellationToken cancellationToken)
        {
            using var stream = await storage.ReadStreamAsync(document, DataKey, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            return TryReadPersistedInfo(reader);
        }

        private async Task PersistTodoCommentsAsync(
            IPersistentStorage storage,
            Document document,
            PersistedTodoCommentInfo info,
            CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new ObjectWriter(memoryStream);

            writer.WriteString(SerializationFormat);
            info.Version.WriteTo(writer);
            writer.WriteString(info.OptionText);

            writer.WriteInt32(info.TodoComments.Length);
            foreach (var comment in info.TodoComments)
                comment.WriteTo(writer);

            memoryStream.Position = 0;
            await storage.WriteStreamAsync(
                document, DataKey, memoryStream, cancellationToken).ConfigureAwait(false);
        }

        private static PersistedTodoCommentInfo? TryReadPersistedInfo(ObjectReader reader)
        {
            if (reader == null)
                return null;

            try
            {
                var serializationFormat = reader.ReadString();
                if (serializationFormat != SerializationFormat)
                    return null;

                var version = VersionStamp.ReadFrom(reader);
                var optionText = reader.ReadString();

                var count = reader.ReadInt32();
                using var _ = ArrayBuilder<TodoCommentInfo>.GetInstance(out var comments);
                for (int i = 0; i < count; i++)
                    comments.Add(TodoCommentInfo.ReadFrom(reader));

                return new PersistedTodoCommentInfo
                {
                    Version = version,
                    OptionText = optionText,
                    TodoComments = comments.ToImmutable(),
                };
            }
            catch
            {
            }
            return null;
        }

        private class PersistedTodoCommentInfo
        {
            public VersionStamp Version;
            public string? OptionText;
            public ImmutableArray<TodoCommentInfo> TodoComments;
        }
    }
}
