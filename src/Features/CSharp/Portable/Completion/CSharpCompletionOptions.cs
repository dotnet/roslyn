// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
