// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Serialization type used to pass information to/from OOP and VS.
    /// </summary>
    internal struct TodoCommentData : IEquatable<TodoCommentData>
    {
        public int Priority;
        public string Message;
        public DocumentId DocumentId;
        public string? MappedFilePath;
        public string? OriginalFilePath;
        public int MappedLine;
        public int MappedColumn;
        public int OriginalLine;
        public int OriginalColumn;

        public override bool Equals(object? obj)
            => obj is TodoCommentData other && Equals(other);

        public override int GetHashCode()
            => GetHashCode(this);

        public override string ToString()
            => $"{Priority} {Message} {MappedFilePath ?? ""} ({MappedLine.ToString()}, {MappedColumn.ToString()}) [original: {OriginalFilePath ?? ""} ({OriginalLine.ToString()}, {OriginalColumn.ToString()})";

        public bool Equals(TodoCommentData right)
        {
            return DocumentId == right.DocumentId &&
                   Priority == right.Priority &&
                   Message == right.Message &&
                   OriginalLine == right.OriginalLine &&
                   OriginalColumn == right.OriginalColumn;
        }

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
        {
            return new TodoCommentData
            {
                Priority = reader.ReadInt32(),
                Message = reader.ReadString(),
                DocumentId = DocumentId.ReadFrom(reader),
                MappedFilePath = reader.ReadString(),
                OriginalFilePath = reader.ReadString(),
                MappedLine = reader.ReadInt32(),
                MappedColumn = reader.ReadInt32(),
                OriginalLine = reader.ReadInt32(),
                OriginalColumn = reader.ReadInt32(),
            };
        }
    }
}
