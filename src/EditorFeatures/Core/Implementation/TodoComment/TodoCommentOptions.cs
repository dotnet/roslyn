// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    internal static class TodoCommentOptions
    {
        public const string OptionName = "TaskList/Tokens";

        // This is serialized by the CommentTaskTokenSerializer in the Visual Studio layer, since it's backed by another component
        [ExportOption]
        public static readonly Option<string> TokenList = new Option<string>(OptionName, "Token List", defaultValue: "HACK:1|TODO:1|UNDONE:1|UnresolvedMergeConflict:0");
    }
}
