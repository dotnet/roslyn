// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
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
            BindToOption(ShowSnippets, CSharpCompletionOptions.IncludeSnippets);
            BindToOption(ShowKeywords, CompletionOptions.IncludeKeywords, LanguageNames.CSharp);

            BindToOption(Show_completion_list_after_a_character_is_typed, CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp);
            Show_completion_list_after_a_character_is_deleted.IsChecked = this.OptionService.GetOption(
                CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp) == true;

            BindToOption(Never_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.CSharp);
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptions.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.CSharp);
            BindToOption(Always_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.CSharp);
        }

        private void BringUpOnIdentifier_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowKeywords.IsEnabled = false;
            ShowSnippets.IsEnabled = false;

            ShowKeywords.IsChecked = true;
            ShowSnippets.IsChecked = true;
        }

        private void BringUpOnIdentifier_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowKeywords.IsEnabled = true;
            ShowSnippets.IsEnabled = true;
        }

        private void Show_completion_list_after_a_character_is_deleted_Checked(object sender, RoutedEventArgs e)
        {
            this.OptionService.SetOptions(
                this.OptionService.GetOptions().WithChangedOption(
                    CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp, value: true));
        }

        private void Show_completion_list_after_a_character_is_deleted_Unchecked(object sender, RoutedEventArgs e)
        {
            this.OptionService.SetOptions(
                this.OptionService.GetOptions().WithChangedOption(
                    CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp, value: false));
        }
    }
}
