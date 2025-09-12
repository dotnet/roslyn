// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;

/// <summary>
/// Interaction logic for FormattingIndentationOptionControl.xaml
/// </summary>
internal sealed class IndentationViewModel : AbstractOptionPreviewViewModel
{
    private const string BlockContentPreview = """
        class C {
        //[
            int Method() {
                int x;
                int y;
            }
        //]
        }
        """;

    private const string IndentBracePreview = """
        class C {
        //[
            int Method() {
                return 0;
            }
        //]
        }
        """;

    private const string SwitchCasePreview = """
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
        }
        """;

    private const string SwitchCaseWhenBlockPreview = """
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
        }
        """;

    private const string GotoLabelPreview = """
        class MyClass
        {
            int Method(int goo){
        //[
            MyLabel:
                goto MyLabel;
                return 0;
        //]
            }
        }
        """;

    public IndentationViewModel(OptionStore optionStore, IServiceProvider serviceProvider) : base(optionStore, serviceProvider, LanguageNames.CSharp)
    {
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.IndentBlock, CSharpVSResources.Indent_block_contents, BlockContentPreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.IndentBraces, CSharpVSResources.Indent_open_and_close_braces, IndentBracePreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.IndentSwitchCaseSection, CSharpVSResources.Indent_case_contents, SwitchCasePreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock, CSharpVSResources.Indent_case_contents_when_block, SwitchCaseWhenBlockPreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.IndentSwitchSection, CSharpVSResources.Indent_case_labels, SwitchCasePreview, this, optionStore));

        Items.Add(new TextBlock() { Text = CSharpVSResources.Label_Indentation });

        Items.Add(new RadioButtonViewModel<LabelPositionOptionsInternal>(CSharpVSResources.Place_goto_labels_in_leftmost_column, GotoLabelPreview, "goto", LabelPositionOptionsInternal.LeftMost, CSharpFormattingOptions2.LabelPositioning, this, optionStore));
        Items.Add(new RadioButtonViewModel<LabelPositionOptionsInternal>(CSharpVSResources.Indent_labels_normally, GotoLabelPreview, "goto", LabelPositionOptionsInternal.NoIndent, CSharpFormattingOptions2.LabelPositioning, this, optionStore));
        Items.Add(new RadioButtonViewModel<LabelPositionOptionsInternal>(CSharpVSResources.Place_goto_labels_one_indent_less_than_current, GotoLabelPreview, "goto", LabelPositionOptionsInternal.OneLess, CSharpFormattingOptions2.LabelPositioning, this, optionStore));
    }
}
