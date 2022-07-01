// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal abstract class FSharpCompletionProviderBase : CompletionProvider
    {
        public sealed override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
            => ShouldTriggerCompletionImpl(text, caretPosition, trigger);

        internal sealed override bool ShouldTriggerCompletion(HostLanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CompletionOptions options, OptionSet passthroughOptions)
            => ShouldTriggerCompletionImpl(text, caretPosition, trigger);

        protected abstract bool ShouldTriggerCompletionImpl(SourceText text, int caretPosition, CompletionTrigger trigger);
    }
}
