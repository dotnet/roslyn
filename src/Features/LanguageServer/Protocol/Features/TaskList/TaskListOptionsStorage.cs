﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.LanguageServer.Features.TaskList
{
    internal static class TaskListOptionsStorage
    {
        public static readonly Option2<ImmutableArray<string>> Descriptors = new(
            "TodoCommentOptions",
            "TokenList",
            TaskListOptions.Default.Descriptors,
            new RoamingProfileStorageLocation("Microsoft.VisualStudio.ErrorListPkg.Shims.TaskListOptions.CommentTokens"));

        public static readonly Option2<bool> ComputeTaskListItemsForClosedFiles = new(
            "TodoCommentOptions",
            "ComputeTaskListItemsForClosedFiles",
            defaultValue: true,
            new RoamingProfileStorageLocation($"TextEditor.Specific.ComputeTaskListItemsForClosedFiles"));

        public static TaskListOptions GetTaskListOptions(this IGlobalOptionService globalOptions)
            => new()
            {
                Descriptors = globalOptions.GetOption(Descriptors),
                ComputeForClosedFiles = globalOptions.GetOption(ComputeTaskListItemsForClosedFiles)
            };
    }
}
