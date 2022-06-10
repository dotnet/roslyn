// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    internal static class CSharpCompletionOptions
    {
        [Obsolete("This option is superceded by CompletionOptions.Metadata.EnterKeyBehavior")]
        public static readonly Option2<bool> AddNewLineOnEnterAfterFullyTypedWord = new Option2<bool>(nameof(CSharpCompletionOptions), nameof(AddNewLineOnEnterAfterFullyTypedWord), defaultValue: false);

        [Obsolete("This option is superceded by CompletionOptions.Metadata.SnippetsBehavior")]
        public static readonly Option2<bool> IncludeSnippets = new Option2<bool>(nameof(CSharpCompletionOptions), nameof(IncludeSnippets), defaultValue: true);
    }
}
