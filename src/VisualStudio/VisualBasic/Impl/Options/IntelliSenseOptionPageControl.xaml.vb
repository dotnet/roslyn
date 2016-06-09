' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Friend Class IntelliSenseOptionPageControl
        Inherits AbstractOptionPageControl

        Public Sub New(serviceProvider As IServiceProvider)
            MyBase.New(serviceProvider)
            InitializeComponent()

            BindToOption(Show_completion_item_filters, CompletionOptions.ShowCompletionItemFilters, LanguageNames.VisualBasic)
            BindToOption(Highlight_matching_portions_of_completion_list_items, CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, LanguageNames.VisualBasic)

            BindToOption(Never_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Never, LanguageNames.VisualBasic)
            BindToOption(Only_add_new_line_on_enter_with_whole_word, CompletionOptions.EnterKeyBehavior, EnterKeyRule.AfterFullyTypedWord, LanguageNames.VisualBasic)
            BindToOption(Always_add_new_line_on_enter, CompletionOptions.EnterKeyBehavior, EnterKeyRule.Always, LanguageNames.VisualBasic)
        End Sub
    End Class
End Namespace