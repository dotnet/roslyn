// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class HyperlinkStyleHelper
    {
        // cache hyperlink style
        private static Style s_hyperlinkStyle = null;

        public static void SetVSHyperLinkStyle(this Hyperlink hyperlink)
        {
            hyperlink.Style = GetOrCreateHyperLinkStyle();
        }

        private static Style GetOrCreateHyperLinkStyle()
        {
            if (s_hyperlinkStyle == null)
            {
                s_hyperlinkStyle = CreateHyperLinkStyle();
            }

            return s_hyperlinkStyle;
        }

        private static Style CreateHyperLinkStyle()
        {
            // this completely override existing hyperlink style.
            // I couldn't find one that does this for me or change existing style little bit for my purpose.
            var style = new Style(typeof(Hyperlink));
            style.Setters.Add(new Setter { Property = Hyperlink.ForegroundProperty, Value = GetBrush(EnvironmentColors.ControlLinkTextBrushKey) });
            style.Setters.Add(new Setter { Property = Hyperlink.TextDecorationsProperty, Value = null });

            var mouseOverTrigger = new Trigger { Property = Hyperlink.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter { Property = Hyperlink.ForegroundProperty, Value = GetBrush(EnvironmentColors.ControlLinkTextHoverBrushKey) });
            style.Triggers.Add(mouseOverTrigger);

            var disabledTrigger = new Trigger { Property = Hyperlink.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter { Property = Hyperlink.ForegroundProperty, Value = GetBrush(EnvironmentColors.PanelHyperlinkDisabledBrushKey) });
            style.Triggers.Add(disabledTrigger);

            var enabledTrigger = new Trigger { Property = Hyperlink.IsEnabledProperty, Value = true };
            enabledTrigger.Setters.Add(new Setter { Property = Hyperlink.CursorProperty, Value = Cursors.Hand });
            style.Triggers.Add(enabledTrigger);

            return style;
        }

        private static Brush GetBrush(ThemeResourceKey key)
        {
            if (Application.Current == null)
            {
                return null;
            }

            return (Brush)Application.Current.Resources[key];
        }
    }
}
