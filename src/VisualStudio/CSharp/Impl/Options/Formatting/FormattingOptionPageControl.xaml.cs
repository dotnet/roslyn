// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
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

            BindToOption(FormatWhenTypingCheckBox, AutoFormattingOptions.Metadata.AutoFormattingOnTyping, LanguageNames.CSharp);
            BindToOption(FormatOnCloseBraceCheckBox, AutoFormattingOptions.Metadata.AutoFormattingOnCloseBrace, LanguageNames.CSharp);
            BindToOption(FormatOnSemicolonCheckBox, AutoFormattingOptions.Metadata.AutoFormattingOnSemicolon, LanguageNames.CSharp);
            BindToOption(FormatOnReturnCheckBox, AutoFormattingOptions.Metadata.AutoFormattingOnReturn, LanguageNames.CSharp);
            BindToOption(FormatOnPasteCheckBox, FormattingOptionsMetadata.FormatOnPaste, LanguageNames.CSharp);
        }
    }
}
