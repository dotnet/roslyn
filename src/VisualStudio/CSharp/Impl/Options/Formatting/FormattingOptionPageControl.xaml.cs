// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

            FormatOnSemicolonCheckBox.Content = CSharpVSResources.Automatically_format_statement_on;
            FormatOnCloseBraceCheckBox.Content = CSharpVSResources.Automatically_format_block_on;
            FormatOnPasteCheckBox.Content = CSharpVSResources.Automatically_format_on_paste;

            BindToOption(FormatOnPasteCheckBox, FeatureOnOffOptions.FormatOnPaste, LanguageNames.CSharp);
            BindToOption(FormatOnCloseBraceCheckBox, FeatureOnOffOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp);
            BindToOption(FormatOnSemicolonCheckBox, FeatureOnOffOptions.AutoFormattingOnSemicolon, LanguageNames.CSharp);
        }
    }
}
