// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.SuggestionMode;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
    [ExportLanguageServiceFactory(typeof(CompletionService), LanguageNames.CSharp), Shared]
    internal class CSharpCompletionServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpCompletionServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpCompletionService(languageServices.WorkspaceServices.Workspace);
        }
    }

    internal class CSharpCompletionService : CommonCompletionService
    {
        private readonly Workspace _workspace;
        private readonly ImmutableArray<CompletionProvider> _defaultCompletionProviders;

        public CSharpCompletionService(
            Workspace workspace, ImmutableArray<CompletionProvider>? exclusiveProviders = null)
            : base(workspace, exclusiveProviders)
        {
            _workspace = workspace;

            var defaultCompletionProviders = ImmutableArray.Create<CompletionProvider>(
                new AttributeNamedParameterCompletionProvider(),
                new NamedParameterCompletionProvider(),
                new KeywordCompletionProvider(),
                new SpeculativeTCompletionProvider(),
                new SymbolCompletionProvider(),
                new ExplicitInterfaceMemberCompletionProvider(),
                new ExplicitInterfaceTypeCompletionProvider(),
                new ObjectCreationCompletionProvider(),
                new ObjectInitializerCompletionProvider(),
                new CSharpSuggestionModeCompletionProvider(),
                new EnumAndCompletionListTagCompletionProvider(),
                new CrefCompletionProvider(),
                new SnippetCompletionProvider(),
                new ExternAliasCompletionProvider(),
                new OverrideCompletionProvider(),
                new PartialMethodCompletionProvider(),
                new PartialTypeCompletionProvider(),
                new XmlDocCommentCompletionProvider(),
                new TupleNameCompletionProvider(),
                new DeclarationNameCompletionProvider(),
                new InternalsVisibleToCompletionProvider(),
                new PropertySubpatternCompletionProvider());

            var languageServices = workspace.Services.GetLanguageServices(LanguageNames.CSharp);
            var languagesProvider = languageServices.GetService<IEmbeddedLanguagesProvider>();
            if (languagesProvider != null)
            {
                defaultCompletionProviders = defaultCompletionProviders.Add(
                    new EmbeddedLanguageCompletionProvider(languagesProvider));
            }

            defaultCompletionProviders = defaultCompletionProviders.Add(new TypeImportCompletionProvider());

            _defaultCompletionProviders = defaultCompletionProviders;
        }

        public override string Language => LanguageNames.CSharp;

        protected override ImmutableArray<CompletionProvider> GetBuiltInProviders()
        {
            return _defaultCompletionProviders;
        }

        public override TextSpan GetDefaultCompletionListSpan(SourceText text, int caretPosition)
        {
            return CompletionUtilities.GetCompletionItemSpan(text, caretPosition);
        }

        private CompletionRules _latestRules = CompletionRules.Default;

        public override CompletionRules GetRules()
        {
            var options = _workspace.Options;

            var enterRule = options.GetOption(CompletionOptions.EnterKeyBehavior, LanguageNames.CSharp);
            var snippetRule = options.GetOption(CompletionOptions.SnippetsBehavior, LanguageNames.CSharp);

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
