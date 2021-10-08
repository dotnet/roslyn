// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    [Obsolete]
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

#pragma warning disable CS0612 // Type or member is obsolete
    internal abstract class FSharpCommonCompletionProviderBase : IFSharpCommonCompletionProvider
#pragma warning restore CS0612 // Type or member is obsolete
    {
        public abstract Task ProvideCompletionsAsync(CompletionContext context);

        public abstract bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition);

        public abstract Task<TextChange?> GetTextChangeAsync(
            Func<CompletionItem, char?, CancellationToken, Task<TextChange?>> baseGetTextChangeAsync,
            CompletionItem selectedItem,
            char? ch,
            CancellationToken cancellationToken);

        bool IFSharpCommonCompletionProvider.IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => IsInsertionTrigger(text, insertedCharacterPosition);
    }
}
