// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.TodoComments
{
    internal static class TodoCommentOptionsStorage
    {
        public static readonly Option2<ImmutableArray<string>> TokenList = new(
            "TodoCommentOptions",
            "TokenList",
            TodoCommentOptions.Default.TokenList,
            new RoamingProfileStorageLocation("Microsoft.VisualStudio.ErrorListPkg.Shims.TaskListOptions.CommentTokens"));

        public static TodoCommentOptions GetTodoCommentOptions(this IGlobalOptionService globalOptions)
            => new()
            {
                TokenList = globalOptions.GetOption(TokenList)
            };
    }
}
