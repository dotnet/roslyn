// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class TodoTableItem : TableItem
    {
        public readonly TodoItem Data;

        public TodoTableItem(Workspace workspace, SharedInfoCache cache, TodoItem data)
            : base(workspace, cache)
        {
            Contract.ThrowIfNull(data);
            Data = data;
        }

        public override TableItem WithCache(SharedInfoCache cache)
            => new TodoTableItem(Workspace, cache, Data);

        public override DocumentId DocumentId
            => Data.DocumentId;

        public override ProjectId ProjectId
            => Data.DocumentId.ProjectId;

        public override int GetDeduplicationKey()
            => Hash.Combine(Data.OriginalColumn, Data.OriginalLine);

        public override LinePosition GetOriginalPosition()
            => new LinePosition(Data.OriginalLine, Data.OriginalColumn);

        public override string GetOriginalFilePath()
            => Data.OriginalFilePath;

        public override bool EqualsIgnoringLocation(TableItem other)
        {
            if (!(other is TodoTableItem otherTodoItem))
            {
                return false;
            }

            var data = Data;
            var otherData = otherTodoItem.Data;
            return data.DocumentId == otherData.DocumentId && data.Message == otherData.Message;
        }
    }
}
