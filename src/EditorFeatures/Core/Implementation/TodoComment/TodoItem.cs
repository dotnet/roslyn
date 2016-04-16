// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class TodoItem
    {
        public TodoItem(
            int priority,
            string message,
            Workspace workspace,
            DocumentId documentId,
            int mappedLine,
            int originalLine,
            int mappedColumn,
            int originalColumn,
            string mappedFilePath,
            string originalFilePath)
        {
            Priority = priority;
            Message = message;

            Workspace = workspace;
            DocumentId = documentId;

            MappedLine = mappedLine;
            MappedColumn = mappedColumn;
            MappedFilePath = mappedFilePath;

            OriginalLine = originalLine;
            OriginalColumn = originalColumn;
            OriginalFilePath = originalFilePath;
        }

        public int Priority { get; }

        public string Message { get; }

        public Workspace Workspace { get; }

        public DocumentId DocumentId { get; }

        public string MappedFilePath { get; }

        public string OriginalFilePath { get; }

        public int MappedLine { get; }

        public int MappedColumn { get; }

        public int OriginalLine { get; }

        public int OriginalColumn { get; }

        public override bool Equals(object obj)
        {
            TodoItem other = obj as TodoItem;
            if (other == null)
            {
                return false;
            }

            return Equals(this, other);
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public override string ToString()
        {
            return
                $"{Priority} {Message} {MappedFilePath ?? ""} ({MappedLine.ToString()}, {MappedColumn.ToString()}) [original: {OriginalFilePath ?? ""} ({OriginalLine.ToString()}, {OriginalColumn.ToString()})";
        }

        public static bool Equals(TodoItem item1, TodoItem item2)
        {
            if (item1.DocumentId != null && item2.DocumentId != null)
            {
                return item1.DocumentId == item2.DocumentId &&
                       item1.Priority == item2.Priority &&
                       item1.Message == item2.Message &&
                       item1.OriginalLine == item2.OriginalLine &&
                       item1.OriginalColumn == item2.OriginalColumn;
            }

            return item1.DocumentId == item2.DocumentId &&
                   item1.Priority == item2.Priority &&
                   item1.Message == item2.Message;
        }

        public static int GetHashCode(TodoItem item)
        {
            if (item.DocumentId != null)
            {
                return Hash.Combine(item.DocumentId,
                       Hash.Combine(item.Priority,
                       Hash.Combine(item.Message,
                       Hash.Combine(item.OriginalLine,
                       Hash.Combine(item.OriginalColumn, 0)))));
            }

            return Hash.Combine(item.Message, item.Priority);
        }
    }
}
