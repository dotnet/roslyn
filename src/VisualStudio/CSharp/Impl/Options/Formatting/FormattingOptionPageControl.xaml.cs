// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using System.Runtime.CompilerServices;

// 🐉 The XAML markup compiler does not recognize InternalsVisibleTo. However, since it allows type
// forwarding, we use TypeForwardedTo to make CodeStyleNoticeTextBlock appear to the markup compiler
// as an internal type in the current assembly instead of an internal type in one of the referenced
// assemblies.
[assembly: TypeForwardedTo(typeof(CodeStyleNoticeTextBlock))]

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    /// <summary>
    /// Interaction logic for FormattingOptionPageControl.xaml
    /// </summary>
    internal partial class FormattingOptionPageControl : AbstractOptionPageControl
    {
        public FormattingOptionPageControl(OptionStore optionStore) : base(optionStore)
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
            BindToOption(FormatOnReturnCheckBox, FormattingOptions.AutoFormattingOnReturn, LanguageNames.CSharp);
            BindToOption(FormatOnPasteCheckBox, FeatureOnOffOptions.FormatOnPaste, LanguageNames.CSharp);
        }
    }
}
