// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common.ColumnDefinitions.CodeStyle;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View.ColumnDefinitions
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(HelpLink)]
    internal class CodeStyleHelpLinkColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeStyleHelpLinkColumnDefinition()
        {
        }

        public override ImageMoniker DisplayImage => KnownMonikers.F1Help;

        public override string Name => HelpLink;
        // PROTOTYPE: Move to resources to make it localizable.
        public override string DisplayName => "Help link";
        public override double MinWidth => 120;
        public override bool DefaultVisible => false;
        public override bool IsFilterable => false;
        public override bool IsSortable => false;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (!entry.TryGetValue(HelpLink, out string helpLink))
            {
                content = null;
                return false;
            }

            var image = new CrispImage
            {
                Moniker = KnownMonikers.StatusInformation,
                Width = 16.0,
                Height = 16.0,
                ToolTip = helpLink,
                Cursor = Cursors.Hand,
            };

            image.MouseLeftButtonUp += OnButtonClick;
            content = image;
            return true;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            var url = ((CrispImage)sender).ToolTip.ToString();
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Process.Start(uri.ToString());
            }
        }
    }
}
