// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TaskList
{
    /// <summary>
    /// Serialization type used to pass information to/from OOP and VS.
    /// </summary>
    [DataContract]
    internal readonly struct TaskListItem : IEquatable<TaskListItem>
    {
        [DataMember(Order = 0)]
        public readonly int Priority;

        [DataMember(Order = 1)]
        public readonly string Message;

        [DataMember(Order = 2)]
        public readonly DocumentId DocumentId;

        [DataMember(Order = 3)]
        public readonly string? MappedFilePath;

        [DataMember(Order = 4)]
        public readonly FileLinePositionSpan MappedSpan;

        public TaskListItem(
            int priority,
            string message,
            DocumentId documentId,
            FileLinePositionSpan span,
            FileLinePositionSpan mappedSpan)
        {
            Priority = priority;
            Message = message;
            DocumentId = documentId;
            MappedFilePath = mappedFilePath;
            OriginalFilePath = originalFilePath;
            MappedLine = mappedLine;
            MappedColumn = mappedColumn;
            OriginalLine = originalLine;
            OriginalColumn = originalColumn;
        }

        public override int GetHashCode()
            => GetHashCode(this);

        public override string ToString()
            => $"{Priority} {Message} {MappedFilePath ?? ""} ({MappedLine}, {MappedColumn}) [original: {OriginalFilePath ?? ""} ({OriginalLine}, {OriginalColumn})";

        public override bool Equals(object? obj)
            => obj is TaskListItem other && Equals(other);

        public bool Equals(TaskListItem obj)
            => DocumentId == obj.DocumentId &&
               Priority == obj.Priority &&
               Message == obj.Message &&
               Span == obj.Span &&
               MappedSpan == obj.MappedSpan;

        public static int GetHashCode(TaskListItem item)
            => Hash.Combine(item.DocumentId,
               Hash.Combine(item.Priority,
               Hash.Combine(item.Message,
               Hash.Combine(item.OriginalLine,
               Hash.Combine(item.OriginalColumn, 0)))));

        internal void WriteTo(ObjectWriter writer)
        {
            writer.WriteInt32(Priority);
            writer.WriteString(Message);
            DocumentId.WriteTo(writer);
            writer.WriteString(MappedFilePath);
            writer.WriteString(OriginalFilePath);
            writer.WriteInt32(MappedLine);
            writer.WriteInt32(MappedColumn);
            writer.WriteInt32(OriginalLine);
            writer.WriteInt32(OriginalColumn);
        }

        internal static TodoCommentData ReadFrom(ObjectReader reader)
            => new(priority: reader.ReadInt32(),
                   message: reader.ReadString(),
                   documentId: DocumentId.ReadFrom(reader),
                   mappedFilePath: reader.ReadString(),
                   originalFilePath: reader.ReadString(),
                   mappedLine: reader.ReadInt32(),
                   mappedColumn: reader.ReadInt32(),
                   originalLine: reader.ReadInt32(),
                   originalColumn: reader.ReadInt32());
    }
}
