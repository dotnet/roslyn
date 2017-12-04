﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            // options
            var optionsPanel = new StackPanel();
            this.AddOptions(optionsPanel);

            // scroll
            var viewer = new ScrollViewer()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            viewer.Content = optionsPanel;

            // search 
            var searchBox = new TextBox() { MinWidth = 200, HorizontalAlignment = HorizontalAlignment.Stretch };

            var searchButton = new Button() { Content = "Search" };
            searchButton.Click += (o, a) =>
            {
                foreach (var item in optionsPanel.Children.OfType<CheckBox>())
                {
                    var title = item.Content as string;
                    if (title == null)
                    {
                        continue;
                    }

                    // pattern not match
                    if (title.IndexOf(searchBox.Text, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        // hide it
                        item.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }
            };

            var clearButton = new Button() { Content = "Clear" };
            clearButton.Click += (o, a) => optionsPanel.Children.OfType<CheckBox>().Do(c => c.Visibility = Visibility.Visible);

            var searchPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            searchPanel.Children.Add(searchBox);
            searchPanel.Children.Add(searchButton);
            searchPanel.Children.Add(clearButton);

            // button
            var checkAllButton = new Button() { Content = "Check All" };
            checkAllButton.Click += (o, a) => optionsPanel.Children.OfType<CheckBox>().Where(c => c.Visibility == Visibility.Visible).Do(c => c.IsChecked = true);

            var uncheckAllButton = new Button() { Content = "Uncheck All" };
            uncheckAllButton.Click += (o, a) => optionsPanel.Children.OfType<CheckBox>().Where(c => c.Visibility == Visibility.Visible).Do(c => c.IsChecked = false);

            var selectionPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            selectionPanel.Children.Add(checkAllButton);
            selectionPanel.Children.Add(uncheckAllButton);

            // main panel
            var mainPanel = new DockPanel() { HorizontalAlignment = HorizontalAlignment.Stretch };
            mainPanel.Children.Add(searchPanel);
            mainPanel.Children.Add(selectionPanel);
            mainPanel.Children.Add(viewer);

            DockPanel.SetDock(searchPanel, Dock.Top);
            DockPanel.SetDock(selectionPanel, Dock.Bottom);

            this.Content = mainPanel;
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

        protected void AddOption(Panel panel, IOption option, string additional = null)
        {
            var uiElement = CreateControl(option, additional: additional);
            if (uiElement != null)
            {
                panel.Children.Add(uiElement);
            }
        }

        protected void AddPerLanguageOption(Panel panel, IOption option, string languageName, string additional = null)
        {
            var uiElement = CreateControl(option, languageName, additional);
            if (uiElement != null)
            {
                panel.Children.Add(uiElement);
            }
        }

        private UIElement CreateControl(IOption option, string languageName = null, string additional = null)
        {
            if (option.Type == typeof(bool))
            {
                var checkBox = new CheckBox() { Content = option.Name + GetLanguage(languageName) + GetAdditionalText(additional) };
                BindToCheckBox(checkBox, option, languageName);
                return checkBox;
            }

            if (option.Type == typeof(int))
            {
                var label = new Label() { Content = option.Name + GetLanguage(languageName) + GetAdditionalText(additional) };
                var textBox = new TextBox();
                BindToTextBox(textBox, option, languageName);

                var panel = new StackPanel();
                panel.Children.Add(label);
                panel.Children.Add(textBox);

                return panel;
            }

            return null;
        }

        private string GetAdditionalText(string additional)
        {
            if (additional == null)
            {
                return string.Empty;
            }

            return " [" + additional + "]";
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
