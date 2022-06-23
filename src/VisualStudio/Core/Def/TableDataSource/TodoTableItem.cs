// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class TodoTableItem : TableItem
    {
        public readonly TodoCommentData Data;

        private TodoTableItem(
            Workspace workspace,
            TodoCommentData data,
            string? projectName,
            Guid projectGuid,
            string[] projectNames,
            Guid[] projectGuids)
            : base(workspace, projectName, projectGuid, projectNames, projectGuids)
        {
            Data = data;
        }

        public static TodoTableItem Create(Workspace workspace, TodoCommentData data)
        {
            GetProjectNameAndGuid(workspace, data.DocumentId.ProjectId, out var projectName, out var projectGuid);
            return new TodoTableItem(workspace, data, projectName, projectGuid, projectNames: Array.Empty<string>(), projectGuids: Array.Empty<Guid>());
        }

        public override TableItem WithAggregatedData(string[] projectNames, Guid[] projectGuids)
            => new TodoTableItem(Workspace, Data, projectName: null, projectGuid: Guid.Empty, projectNames, projectGuids);

        public override DocumentId DocumentId
            => Data.DocumentId;

        public override ProjectId ProjectId
            => Data.DocumentId.ProjectId;

        public override LinePosition GetOriginalPosition()
            => new(Data.OriginalLine, Data.OriginalColumn);

        public override string? GetOriginalFilePath()
            => Data.OriginalFilePath;

        public override bool EqualsIgnoringLocation(TableItem other)
        {
            if (other is not TodoTableItem otherTodoItem)
            {
                return false;
            }

            var data = Data;
            var otherData = otherTodoItem.Data;
            return data.DocumentId == otherData.DocumentId && data.Message == otherData.Message;
        }

        /// <summary>
        /// Used to group diagnostics that only differ in the project they come from.
        /// We want to avoid displaying diagnostic multuple times when it is reported from 
        /// multi-targeted projects and/or files linked to multiple projects.
        /// </summary>
        internal sealed class GroupingComparer : IEqualityComparer<TodoCommentData>, IEqualityComparer<TodoTableItem>
        {
            public static readonly GroupingComparer Instance = new();

            public bool Equals(TodoCommentData left, TodoCommentData right)
            {
                // We don't need to compare OriginalFilePath since TODO items are only aggregated within a single file.
                return
                    left.OriginalLine == right.OriginalLine &&
                    left.OriginalColumn == right.OriginalColumn;
            }

            public int GetHashCode(TodoCommentData data)
                => Hash.Combine(data.OriginalLine, data.OriginalColumn);

            public bool Equals(TodoTableItem left, TodoTableItem right)
            {
                if (ReferenceEquals(left, right))
                {
                    return true;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                return Equals(left.Data, right.Data);
            }

            public int GetHashCode(TodoTableItem item)
                => GetHashCode(item.Data);
        }
    }
}
