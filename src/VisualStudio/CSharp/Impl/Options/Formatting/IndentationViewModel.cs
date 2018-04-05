// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    /// <summary>
    /// Interaction logic for FormattingIndentationOptionControl.xaml
    /// </summary>
    internal class IndentationViewModel : AbstractOptionPreviewViewModel
    {
        private const string BlockContentPreview = @"
class C {
//[
    int Method() {
        int x;
        int y;
    }
//]
}";

        private const string IndentBracePreview = @"
class C {
//[
    int Method() {
        return 0;
    }
//]
}";

        private const string SwitchCasePreview = @"
class MyClass
{
    int Method(int goo){
//[
        switch (goo){
        case 2:
            break;
        }
//]
    }
}";

        private const string SwitchCaseWhenBlockPreview = @"
class MyClass
{
    int Method(int goo){
//[
        switch (goo){
        case 2:
            {
                break;
            }
        }
//]
    }
}";

        private const string GotoLabelPreview = @"
class MyClass
{
    int Method(int goo){
//[
    MyLabel:
        goto MyLabel;
        return 0;
//]
    }
}";

        public IndentationViewModel(OptionSet options, IServiceProvider serviceProvider) : base(options, serviceProvider, LanguageNames.CSharp)
        {
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentBlock, CSharpVSResources.Indent_block_contents, BlockContentPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentBraces, CSharpVSResources.Indent_open_and_close_braces, IndentBracePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentSwitchCaseSection, CSharpVSResources.Indent_case_contents, SwitchCasePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentSwitchCaseSectionWhenBlock, CSharpVSResources.Indent_case_contents_when_block, SwitchCaseWhenBlockPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentSwitchSection, CSharpVSResources.Indent_case_labels, SwitchCasePreview, this, options));

            Items.Add(new TextBlock() { Text = CSharpVSResources.Label_Indentation });

            Items.Add(new RadioButtonViewModel<LabelPositionOptions>(CSharpVSResources.Place_goto_labels_in_leftmost_column, GotoLabelPreview, "goto", LabelPositionOptions.LeftMost, CSharpFormattingOptions.LabelPositioning, this, options));
            Items.Add(new RadioButtonViewModel<LabelPositionOptions>(CSharpVSResources.Indent_labels_normally, GotoLabelPreview, "goto", LabelPositionOptions.NoIndent, CSharpFormattingOptions.LabelPositioning, this, options));
            Items.Add(new RadioButtonViewModel<LabelPositionOptions>(CSharpVSResources.Place_goto_labels_one_indent_less_than_current, GotoLabelPreview, "goto", LabelPositionOptions.OneLess, CSharpFormattingOptions.LabelPositioning, this, options));
        }
    }
}
