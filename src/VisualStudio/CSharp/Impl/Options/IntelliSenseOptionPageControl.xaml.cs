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

            BindToOption(InsertNewlineOnEnterWithWholeWord, CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord);
            BindToOption(ShowSnippets, CSharpCompletionOptions.IncludeSnippets);
            BindToOption(ShowKeywords, CompletionOptions.IncludeKeywords, LanguageNames.CSharp);
            BindToOption(BringUpOnIdentifier, CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp);
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
    }
}
