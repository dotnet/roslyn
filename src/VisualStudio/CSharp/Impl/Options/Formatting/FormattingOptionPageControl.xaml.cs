// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    /// <summary>
    /// Interaction logic for FormattingOptionPageControl.xaml
    /// </summary>
    internal partial class FormattingOptionPageControl : AbstractOptionPageControl
    {
        public FormattingOptionPageControl(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();

            FormatWhenTypingCheckBox.Content = CSharpVSResources.Automatically_format_when_typing;
            FormatOnSemicolonCheckBox.Content = CSharpVSResources.Automatically_format_statement_on_semicolon;
            FormatOnCloseBraceCheckBox.Content = CSharpVSResources.Automatically_format_block_on_close_brace;
            FormatOnReturnCheckBox.Content = CSharpVSResources.Automatically_format_on_return;
            FormatOnPasteCheckBox.Content = CSharpVSResources.Automatically_format_on_paste;

            BindToOption(FormatWhenTypingCheckBox, FeatureOnOffOptions.AutoFormattingOnTyping, LanguageNames.CSharp);
            BindToOption(FormatOnCloseBraceCheckBox, FeatureOnOffOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp);
            BindToOption(FormatOnSemicolonCheckBox, FeatureOnOffOptions.AutoFormattingOnSemicolon, LanguageNames.CSharp);
            BindToOption(FormatOnReturnCheckBox, FeatureOnOffOptions.AutoFormattingOnReturn, LanguageNames.CSharp);
            BindToOption(FormatOnPasteCheckBox, FeatureOnOffOptions.FormatOnPaste, LanguageNames.CSharp);
            SetNestedCheckboxesEnabled();
        }

        private void FormatWhenTypingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FormatOnCloseBraceCheckBox.IsChecked = true;
            FormatOnSemicolonCheckBox.IsChecked = true;

            SetNestedCheckboxesEnabled();
        }

        private void FormatWhenTypingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FormatOnCloseBraceCheckBox.IsChecked = false;
            FormatOnSemicolonCheckBox.IsChecked = false;

            SetNestedCheckboxesEnabled();
        }

        private void SetNestedCheckboxesEnabled()
        {
            FormatOnCloseBraceCheckBox.IsEnabled = FormatWhenTypingCheckBox.IsChecked == true;
            FormatOnSemicolonCheckBox.IsEnabled = FormatWhenTypingCheckBox.IsChecked == true;
        }
    }
}