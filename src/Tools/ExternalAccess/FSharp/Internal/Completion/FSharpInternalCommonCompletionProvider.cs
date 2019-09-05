// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Completion
{
    internal sealed class FSharpInternalCommonCompletionProvider : CommonCompletionProvider
    {
        private readonly IFSharpCommonCompletionProvider _provider;

        public FSharpInternalCommonCompletionProvider(IFSharpCommonCompletionProvider provider)
        {
            _provider = provider;
        }

        public override Task ProvideCompletionsAsync(CompletionContext context)
        {
            return _provider.ProvideCompletionsAsync(context);
        }

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return _provider.GetTextChangeAsync(base.GetTextChangeAsync, selectedItem, ch, cancellationToken);
        }

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            return _provider.IsInsertionTrigger(text, insertedCharacterPosition, options);
        }
    }
}
