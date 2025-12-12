// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal partial class CodeStyleNoticeTextBlock : TextBlock
{
    private const string UseEditorConfigUrl = "https://go.microsoft.com/fwlink/?linkid=866541";

    public CodeStyleNoticeTextBlock()
        => InitializeComponent();

    public static readonly Uri CodeStylePageHeaderLearnMoreUri = new(UseEditorConfigUrl);
    public static string CodeStylePageHeader => ServicesVSResources.Code_style_header_use_editor_config;
    public static string CodeStylePageHeaderLearnMoreText => ServicesVSResources.Learn_more;

    private void LearnMoreHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        if (e.Uri == null)
        {
            return;
        }

        VisualStudioNavigateToLinkService.StartBrowser(e.Uri);
        e.Handled = true;
    }
}
