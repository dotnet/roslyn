// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    internal static class TodoCommentOptions
    {
        public static readonly Option<string> TokenList = new Option<string>(nameof(TodoCommentOptions), nameof(TokenList), defaultValue: "HACK:1|TODO:1|UNDONE:1|UnresolvedMergeConflict:0");
    }

    [ExportOptionProvider, Shared]
    internal class TodoCommentOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public TodoCommentOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            TodoCommentOptions.TokenList);
    }
}
