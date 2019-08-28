// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal static class FSharpCompletionOptions
    {
        public static PerLanguageOption<bool> BlockForCompletionItems => Microsoft.CodeAnalysis.Completion.CompletionOptions.BlockForCompletionItems;
    }
}
