// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class IntelliSenseOptionPageControl : AbstractOptionPageControl
    {
        public IntelliSenseOptionPageControl(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();

            BindToOption(Show_completion_item_filters, CompletionOptions.ShowCompletionItemFilters, LanguageNames.CSharp);
            BindToOption(Highlight_matching_portions_of_completion_list_items, CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, LanguageNames.CSharp);
            BindToOption(BringUpOnIdentifier, CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp);

            BindToOption(Never_include_snippets, CompletionOptions.SnippetsBehavior, SnippetsRule.NeverInclude, LanguageNames.CSharp);
            BindToOption(Always_include_snippets, CompletionOptions.SnippetsBehavior, SnippetsRule.AlwaysInclude, LanguageNames.CSharp);
            BindToOption(Include_snippets_when_question_Tab_is_typed_after_an_identifier, CompletionOptions.SnippetsBehavior, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab, LanguageNames.CSharp);

            BindToOption(Never_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.CSharp);
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptions.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.CSharp);
            BindToOption(Always_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.CSharp);
        }
    }
}
