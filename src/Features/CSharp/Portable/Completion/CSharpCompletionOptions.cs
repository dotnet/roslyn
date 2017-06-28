// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    internal static class CSharpCompletionOptions
    {
        [Obsolete("This option is superceded by CompletionOptions.EnterKeyBehavior")]
        public static readonly Option<bool> AddNewLineOnEnterAfterFullyTypedWord = new Option<bool>(nameof(CSharpCompletionOptions), nameof(AddNewLineOnEnterAfterFullyTypedWord), defaultValue: false);

        [Obsolete("This option is superceded by CompletionOptions.SnippetsBehavior")]
        public static readonly Option<bool> IncludeSnippets = new Option<bool>(nameof(CSharpCompletionOptions), nameof(IncludeSnippets), defaultValue: true);
    }
}
