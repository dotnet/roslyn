// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    /// <summary>
    /// Interaction logic for FormattingWrappingOptionPage.xaml
    /// </summary>
    internal class WrappingViewModel : AbstractOptionPreviewViewModel
    {
        private const string s_blockPreview = @"
class C
{
//[
    public int Goo { get; set; }
//]    
}";

        private const string s_declarationPreview = @"
class C{
    void goo()
    {
//[
        int i = 0; string name = ""John"";
//]
    }
}";

        public WrappingViewModel(OptionStore optionStore, IServiceProvider serviceProvider) : base(optionStore, serviceProvider, LanguageNames.CSharp)
        {
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.WrappingPreserveSingleLine, CSharpVSResources.Leave_block_on_single_line, s_blockPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, CSharpVSResources.Leave_statements_and_member_declarations_on_the_same_line, s_declarationPreview, this, optionStore));
        }
    }
}
