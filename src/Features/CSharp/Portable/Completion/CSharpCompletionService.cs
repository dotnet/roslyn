// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.SuggestionMode;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    [ExportLanguageServiceFactory(typeof(CompletionService), LanguageNames.CSharp), Shared]
    internal class CSharpCompletionServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpCompletionService(languageServices.WorkspaceServices.Workspace);
        }
    }

    internal class CSharpCompletionService : CommonCompletionService
    {
        private readonly ImmutableArray<CompletionProvider> _defaultCompletionProviders =
            ImmutableArray.Create<CompletionProvider>(
                new AttributeNamedParameterCompletionProvider(),
                new NamedParameterCompletionProvider(),
                new KeywordCompletionProvider(),
                new SymbolCompletionProvider(),
                new ExplicitInterfaceCompletionProvider(),
                new ObjectCreationCompletionProvider(),
                new ObjectInitializerCompletionProvider(),
                new SpeculativeTCompletionProvider(),
                new CSharpSuggestionModeCompletionProvider(),
                new EnumAndCompletionListTagCompletionProvider(),
                new CrefCompletionProvider(),
                new SnippetCompletionProvider(),
                new ExternAliasCompletionProvider(),
                new OverrideCompletionProvider(),
                new PartialCompletionProvider(),
                new XmlDocCommentCompletionProvider()
            );

        private readonly Workspace _workspace;

        public CSharpCompletionService(
            Workspace workspace, ImmutableArray<CompletionProvider>? exclusiveProviders = null)
            : base(workspace, exclusiveProviders)
        {
            _workspace = workspace;
        }

        public override string Language
        {
            get { return LanguageNames.CSharp; }
        }

        protected override ImmutableArray<CompletionProvider> GetBuiltInProviders()
        {
            return _defaultCompletionProviders;
        }

        public override TextSpan GetDefaultItemSpan(SourceText text, int caretPosition)
        {
            return CompletionUtilities.GetCompletionItemSpan(text, caretPosition);
        }

        private CompletionRules _latestRules = CompletionRules.Default;

        public override CompletionRules GetRules()
        {
            var options = _workspace.Options;

            var rule = options.GetOption(CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord) 
                ? EnterKeyRule.AfterFullyTypedWord 
                : EnterKeyRule.Never;

            // use interlocked + stored rules to reduce # of times this gets created when option is different than default
            var newRules = _latestRules.WithDefaultEnterKeyRule(rule);

            Interlocked.Exchange(ref _latestRules, newRules);

            return newRules;
        }
    }
}
