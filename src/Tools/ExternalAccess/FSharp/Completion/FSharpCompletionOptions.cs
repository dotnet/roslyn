// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal static class FSharpCompletionOptions
    {
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<bool> BlockForCompletionItems { get; } = ((PerLanguageOption<bool>)Microsoft.CodeAnalysis.Completion.CompletionOptions.BlockForCompletionItems2)!;
    }
}
