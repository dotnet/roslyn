// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    internal class TodoTaskItem : TaskItem
    {
        public int Priority { get; }

        public TodoTaskItem(
            int priority,
            string message,
            Workspace workspace,
            DocumentId documentId,
            int mappedLine,
            int originalLine,
            int mappedColumn,
            int originalColumn,
            string mappedFilePath,
            string originalFilePath) :
            base(message, workspace, documentId,
                 mappedLine, originalLine, mappedColumn, originalColumn, mappedFilePath, originalFilePath)
        {
            this.Priority = priority;
        }

        public override bool Equals(object obj)
        {
            TodoTaskItem other = obj as TodoTaskItem;
            if (other == null)
            {
                return false;
            }

            return base.Equals(obj) && this.Priority == other.Priority;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(base.GetHashCode(), this.Priority);
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", this.Priority.ToString(), base.ToString());
        }
    }
}
