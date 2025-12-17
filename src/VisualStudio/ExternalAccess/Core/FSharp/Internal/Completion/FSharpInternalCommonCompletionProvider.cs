// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Completion;

internal sealed class FSharpInternalCommonCompletionProvider : CommonCompletionProvider
{
    private readonly FSharpCommonCompletionProviderBase _provider;

    public FSharpInternalCommonCompletionProvider(FSharpCommonCompletionProviderBase provider)
    {
        _provider = provider;
    }

    internal override string Language => LanguageNames.FSharp;

    public override Task ProvideCompletionsAsync(CompletionContext context)
    {
        return _provider.ProvideCompletionsAsync(context);
    }

    protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
    {
        return _provider.GetTextChangeAsync(base.GetTextChangeAsync, selectedItem, ch, cancellationToken);
    }

    public override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, CompletionOptions options)
    {
        return _provider.IsInsertionTrigger(text, insertedCharacterPosition);
    }
}
