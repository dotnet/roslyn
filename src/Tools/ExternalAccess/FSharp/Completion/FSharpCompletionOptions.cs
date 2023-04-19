// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal static class FSharpCompletionOptions
    {
        public static PerLanguageOption<bool> BlockForCompletionItems { get; } = (PerLanguageOption<bool>)CompletionOptions.BlockForCompletionItems2;
    }
}
