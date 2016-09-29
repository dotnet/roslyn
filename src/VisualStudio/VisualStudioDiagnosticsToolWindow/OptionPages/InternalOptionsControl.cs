// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class InternalOptionsControl : AbstractOptionPageControl
    {
        private readonly string _featureOptionName;

        public InternalOptionsControl(string featureOptionName, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _featureOptionName = featureOptionName;

            var panel = new StackPanel();
            this.AddOptions(panel);

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

        protected virtual void AddOptions(Panel panel)
        {
            foreach (var option in OptionService.GetRegisteredOptions().Where(o => o.Feature == _featureOptionName).OrderBy(o => o.Name))
            {
                if (!option.IsPerLanguage)
                {
                    AddOption(panel, option);
                }
                else
                {
                    AddPerLanguageOption(panel, option, LanguageNames.CSharp);
                    AddPerLanguageOption(panel, option, LanguageNames.VisualBasic);
                }
            }
        }

        private void AddOption(Panel panel, IOption option)
        {
            var uiElement = CreateControl(option);
            if (uiElement != null)
            {
                panel.Children.Add(uiElement);
            }
        }

        private void AddPerLanguageOption(Panel panel, IOption option, string languageName)
        {
            var uiElement = CreateControl(option, languageName);
            if (uiElement != null)
            {
                panel.Children.Add(uiElement);
            }
        }

        private UIElement CreateControl(IOption option, string languageName = null)
        {
            if (option.Type == typeof(bool))
            {
                var checkBox = new CheckBox() { Content = option.Name + GetLanguage(languageName) };
                BindToCheckBox(checkBox, option, languageName);
                return checkBox;
            }

            if (option.Type == typeof(int))
            {
                var label = new Label() { Content = option.Name + GetLanguage(languageName) };
                var textBox = new TextBox();
                BindToTextBox(textBox, option, languageName);

                var panel = new StackPanel();
                panel.Children.Add(label);
                panel.Children.Add(textBox);

                return panel;
            }

            return null;
        }

        private string GetLanguage(string languageName)
        {
            if (languageName == null)
            {
                return string.Empty;
            }

            return " [" + languageName + "]";
        }

        private void BindToCheckBox(CheckBox checkBox, IOption option, string languageName = null)
        {
            if (languageName == null)
            {
                BindToOption(checkBox, (Option<bool>)option);
                return;
            }

            BindToOption(checkBox, (PerLanguageOption<bool>)option, languageName);
        }

        private void BindToTextBox(TextBox textBox, IOption option, string languageName = null)
        {
            if (languageName == null)
            {
                BindToOption(textBox, (Option<int>)option);
                return;
            }

            BindToOption(textBox, (PerLanguageOption<int>)option, languageName);
        }
    }
}
