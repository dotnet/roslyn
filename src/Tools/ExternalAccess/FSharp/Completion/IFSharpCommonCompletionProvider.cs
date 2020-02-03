// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
