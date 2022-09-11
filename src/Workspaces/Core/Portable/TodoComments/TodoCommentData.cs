// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Serialization type used to pass information to/from OOP and VS.
    /// </summary>
    [DataContract]
    internal readonly struct TodoCommentData : IEquatable<TodoCommentData>
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
        public readonly string? OriginalFilePath;

        [DataMember(Order = 5)]
        public readonly int MappedLine;

        [DataMember(Order = 6)]
        public readonly int MappedColumn;

        [DataMember(Order = 7)]
        public readonly int OriginalLine;

        [DataMember(Order = 8)]
        public readonly int OriginalColumn;

        public TodoCommentData(int priority, string message, DocumentId documentId, string? mappedFilePath, string? originalFilePath, int mappedLine, int mappedColumn, int originalLine, int originalColumn)
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

        public override bool Equals(object? obj)
            => obj is TodoCommentData other && Equals(other);

        public override int GetHashCode()
            => GetHashCode(this);

        public override string ToString()
            => $"{Priority} {Message} {MappedFilePath ?? ""} ({MappedLine}, {MappedColumn}) [original: {OriginalFilePath ?? ""} ({OriginalLine}, {OriginalColumn})";

        public bool Equals(TodoCommentData right)
            => DocumentId == right.DocumentId &&
               Priority == right.Priority &&
               Message == right.Message &&
               OriginalLine == right.OriginalLine &&
               OriginalColumn == right.OriginalColumn;

        public static int GetHashCode(TodoCommentData item)
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
