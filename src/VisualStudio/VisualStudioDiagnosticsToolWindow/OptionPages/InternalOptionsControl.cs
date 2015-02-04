// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class InternalOptionsControl : AbstractOptionPageControl
    {
        public InternalOptionsControl(string featureOptionName, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            var panel = new StackPanel();
            foreach (var option in OptionService.GetRegisteredOptions().Where(o => o.Feature == featureOptionName).OrderBy(o => o.Name))
            {
                if (!option.IsPerLanguage)
                {
                    var checkBox = new CheckBox() { Content = option.Name };
                    BindToOption(checkBox, (Option<bool>)option);
                    panel.Children.Add(checkBox);
                }
                else
                {
                    AddPerLanguageOption(panel, option, LanguageNames.CSharp);
                    AddPerLanguageOption(panel, option, LanguageNames.VisualBasic);
                }
            }

            var viewer = new ScrollViewer();
            viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            var checkAllButton = new Button() { Content = "Check All" };
            checkAllButton.Click += (o, a) => panel.Children.OfType<CheckBox>().Do(c => c.IsChecked = true);

            var uncheckAllButton = new Button() { Content = "Uncheck All" };
            uncheckAllButton.Click += (o, a) => panel.Children.OfType<CheckBox>().Do(c => c.IsChecked = false);

            var selectionPanel = new StackPanel();
            selectionPanel.Children.Add(checkAllButton);
            selectionPanel.Children.Add(uncheckAllButton);

            panel.Children.Add(selectionPanel);

            viewer.Content = panel;

            this.Content = viewer;
        }

        private void AddPerLanguageOption(StackPanel panel, IOption option, string language)
        {
            var checkBox = new CheckBox() { Content = option.Name + " [" + language + "]" };
            BindToOption(checkBox, (PerLanguageOption<bool>)option, language);
            panel.Children.Add(checkBox);
        }
    }
}
