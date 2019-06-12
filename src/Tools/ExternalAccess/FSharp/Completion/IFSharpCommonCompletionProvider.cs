// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal interface IFSharpCommonCompletionProvider
    {
        Task ProvideCompletionsAsync(CompletionContext context);

        bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options);

        Task<TextChange?> GetTextChangeAsync(
            Func<CompletionItem, char?, CancellationToken, Task<TextChange?>> baseGetTextChangeAsync,
            CompletionItem selectedItem,
            char? ch,
            CancellationToken cancellationToken);
    }
}
