// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class TodoItem : IEquatable<TodoItem>
    {
        public int Priority { get; }
        public string Message { get; }
        public DocumentId DocumentId { get; }
        public string MappedFilePath { get; }
        public string OriginalFilePath { get; }
        public int MappedLine { get; }
        public int MappedColumn { get; }
        public int OriginalLine { get; }
        public int OriginalColumn { get; }

        public TodoItem(
            int priority,
            string message,
            DocumentId documentId,
            int mappedLine,
            int originalLine,
            int mappedColumn,
            int originalColumn,
            string mappedFilePath,
            string originalFilePath)
        {
            Contract.ThrowIfNull(documentId);

            Priority = priority;
            Message = message;

            DocumentId = documentId;

            MappedLine = mappedLine;
            MappedColumn = mappedColumn;
            MappedFilePath = mappedFilePath;

            OriginalLine = originalLine;
            OriginalColumn = originalColumn;
            OriginalFilePath = originalFilePath;
        }

        public override bool Equals(object obj)
            => obj is TodoItem other && Equals(other);

        public override int GetHashCode()
            => GetHashCode(this);

        public override string ToString()
            => $"{Priority} {Message} {MappedFilePath ?? ""} ({MappedLine.ToString()}, {MappedColumn.ToString()}) [original: {OriginalFilePath ?? ""} ({OriginalLine.ToString()}, {OriginalColumn.ToString()})";

        public bool Equals(TodoItem right)
        {
            if (ReferenceEquals(this, right))
            {
                return true;
            }

            if (right is null)
            {
                return false;
            }

            return DocumentId == right.DocumentId &&
                   Priority == right.Priority &&
                   Message == right.Message &&
                   OriginalLine == right.OriginalLine &&
                   OriginalColumn == right.OriginalColumn;
        }

        public static int GetHashCode(TodoItem item)
            => Hash.Combine(item.DocumentId,
               Hash.Combine(item.Priority,
               Hash.Combine(item.Message,
               Hash.Combine(item.OriginalLine,
               Hash.Combine(item.OriginalColumn, 0)))));
    }
}
