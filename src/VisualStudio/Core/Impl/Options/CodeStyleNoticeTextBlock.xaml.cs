// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class CodeStyleNoticeTextBlock : TextBlock
    {
        private const string UseEditorConfigUrl = "https://go.microsoft.com/fwlink/?linkid=866541";

        public CodeStyleNoticeTextBlock()
        {
            InitializeComponent();
        }

        public static readonly Uri CodeStylePageHeaderLearnMoreUri = new Uri(UseEditorConfigUrl);
        public static string CodeStylePageHeader => ServicesVSResources.Code_style_header_use_editor_config;
        public static string CodeStylePageHeaderLearnMoreText => ServicesVSResources.Learn_more;

        private void LearnMoreHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            BrowserHelper.StartBrowser(e.Uri);
            e.Handled = true;
        }
    }
}
