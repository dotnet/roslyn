// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptCompletionProvider : CompletionProvider
    {
        public sealed override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            Debug.Fail("For backwards API compat only, should not be called");
            var defaultOptions = CompletionOptions.Default;
            return ShouldTriggerCompletionImpl(text, caretPosition, trigger, defaultOptions.TriggerOnTypingLetters, defaultOptions.ExpandedCompletionBehavior);
        }

        internal sealed override bool ShouldTriggerCompletion(HostLanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CompletionOptions options, OptionSet passThroughOptions)
            => ShouldTriggerCompletionImpl(text, caretPosition, trigger, options.TriggerOnTypingLetters, options.ExpandedCompletionBehavior);

        protected abstract bool ShouldTriggerCompletionImpl(SourceText text, int caretPosition, CompletionTrigger trigger, bool triggerOnTypingLetters, ExpandedCompletionMode completionMode);
    }
}
