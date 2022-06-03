// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    internal sealed class CSharpCompletionService : CommonCompletionService
    {
        [ExportLanguageServiceFactory(typeof(CompletionService), LanguageNames.CSharp), Shared]
        internal sealed class Factory : ILanguageServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
                => new CSharpCompletionService(languageServices.WorkspaceServices.Workspace);
        }

        private CompletionRules _latestRules = CompletionRules.Default;

        private CSharpCompletionService(Workspace workspace)
            : base(workspace)
        {
        }

        public override string Language => LanguageNames.CSharp;

        public override TextSpan GetDefaultCompletionListSpan(SourceText text, int caretPosition)
            => CompletionUtilities.GetCompletionItemSpan(text, caretPosition);

        internal override CompletionRules GetRules(CompletionOptions options)
        {
            var enterRule = options.EnterKeyBehavior;
            var snippetRule = options.SnippetsBehavior;

            // Although EnterKeyBehavior is a per-language setting, the meaning of an unset setting (Default) differs between C# and VB
            // In C# the default means Never to maintain previous behavior
            if (enterRule == EnterKeyRule.Default)
            {
                enterRule = EnterKeyRule.Never;
            }

            if (snippetRule == SnippetsRule.Default)
            {
                snippetRule = SnippetsRule.AlwaysInclude;
            }

            // use interlocked + stored rules to reduce # of times this gets created when option is different than default
            var newRules = _latestRules.WithDefaultEnterKeyRule(enterRule)
                                       .WithSnippetsRule(snippetRule);

            Interlocked.Exchange(ref _latestRules, newRules);

            return newRules;
        }
    }
}
