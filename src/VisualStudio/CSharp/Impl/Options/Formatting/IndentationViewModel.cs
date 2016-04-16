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
    int Method(int foo){
//[
        switch (foo){
        case 2:
            break;
        }
//]
    }
}";

        private const string GotoLabelPreview = @"
class MyClass
{
    int Method(int foo){
//[
    MyLabel:
        goto MyLabel;
        return 0;
//]
    }
}";

        public IndentationViewModel(OptionSet options, IServiceProvider serviceProvider) : base(options, serviceProvider, LanguageNames.CSharp)
        {
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentBlock, CSharpVSResources.IndentBlock, BlockContentPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentBraces, CSharpVSResources.IndentBraces, IndentBracePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentSwitchCaseSection, CSharpVSResources.IndentSwitchCaseContents, SwitchCasePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.IndentSwitchSection, CSharpVSResources.IndentSwitchCaseLabels, SwitchCasePreview, this, options));

            Items.Add(new TextBlock() { Text = CSharpVSResources.LabelIndentation });

            Items.Add(new RadioButtonViewModel<LabelPositionOptions>(CSharpVSResources.GotoLeftmost, GotoLabelPreview, "goto", LabelPositionOptions.LeftMost, CSharpFormattingOptions.LabelPositioning, this, options));
            Items.Add(new RadioButtonViewModel<LabelPositionOptions>(CSharpVSResources.GotoNoIndent, GotoLabelPreview, "goto", LabelPositionOptions.NoIndent, CSharpFormattingOptions.LabelPositioning, this, options));
            Items.Add(new RadioButtonViewModel<LabelPositionOptions>(CSharpVSResources.GotoOneLess, GotoLabelPreview, "goto", LabelPositionOptions.OneLess, CSharpFormattingOptions.LabelPositioning, this, options));
        }

        internal override bool ShouldPersistOption(OptionKey key)
        {
            return key.Option.Feature == CSharpFormattingOptions.IndentFeatureName;
        }
    }
}
