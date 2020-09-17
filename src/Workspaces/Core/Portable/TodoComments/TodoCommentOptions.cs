// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TodoCommentOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            TodoCommentOptions.TokenList);
    }
}
