// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class AbstractTaskItem : ITaskItem
    {
        public abstract string Message { get; }
        public abstract Workspace Workspace { get; }
        public abstract DocumentId DocumentId { get; }
        public abstract string MappedFilePath { get; }
        public abstract string OriginalFilePath { get; }
        public abstract int MappedLine { get; }
        public abstract int MappedColumn { get; }
        public abstract int OriginalLine { get; }
        public abstract int OriginalColumn { get; }

        public override bool Equals(object obj)
        {
            ITaskItem other = obj as ITaskItem;
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
            return string.Format("{0} {1} ({2}, {3}) [original: {4} ({5}, {6})]",
                Message,
                MappedFilePath ?? "",
                MappedLine.ToString(),
                MappedColumn.ToString(),
                OriginalFilePath ?? "",
                OriginalLine.ToString(),
                OriginalColumn.ToString());
        }

        public static bool Equals(ITaskItem item1, ITaskItem item2)
        {
            if (item1.DocumentId != null && item2.DocumentId != null)
            {
                return item1.DocumentId == item2.DocumentId &&
                       item1.Message == item2.Message &&
                       item1.OriginalLine == item2.OriginalLine &&
                       item1.OriginalColumn == item2.OriginalColumn;
            }

            return item1.DocumentId == item2.DocumentId &&
                   item1.Message == item2.Message;
        }

        public static int GetHashCode(ITaskItem item)
        {
            if (item.DocumentId != null)
            {
                return Hash.Combine(item.DocumentId,
                       Hash.Combine(item.Message,
                       Hash.Combine(item.OriginalLine,
                       Hash.Combine(item.OriginalColumn, 0))));
            }

            return (item.Message ?? string.Empty).GetHashCode();
        }
    }
}
