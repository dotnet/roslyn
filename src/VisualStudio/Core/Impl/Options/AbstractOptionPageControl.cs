// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [System.ComponentModel.DesignerCategory("code")] // this must be fully qualified
    public abstract class AbstractOptionPageControl : UserControl
    {
        internal readonly OptionStore OptionStore;
        private readonly List<BindingExpressionBase> _bindingExpressions = new List<BindingExpressionBase>();

        public AbstractOptionPageControl(OptionStore optionStore)
        {
            InitializeStyles();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            this.OptionStore = optionStore;
        }

        private void InitializeStyles()
        {
            var groupBoxStyle = new System.Windows.Style(typeof(GroupBox));
            groupBoxStyle.Setters.Add(new Setter(GroupBox.PaddingProperty, new Thickness() { Left = 7, Right = 7, Top = 7 }));
            groupBoxStyle.Setters.Add(new Setter(GroupBox.MarginProperty, new Thickness() { Bottom = 3 }));
            groupBoxStyle.Setters.Add(new Setter(GroupBox.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrushKey)));
            Resources.Add(typeof(GroupBox), groupBoxStyle);

            var checkBoxStyle = new System.Windows.Style(typeof(CheckBox));
            checkBoxStyle.Setters.Add(new Setter(CheckBox.MarginProperty, new Thickness() { Bottom = 7 }));
            checkBoxStyle.Setters.Add(new Setter(CheckBox.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrushKey)));
            Resources.Add(typeof(CheckBox), checkBoxStyle);

            var textBoxStyle = new System.Windows.Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(TextBox.MarginProperty, new Thickness() { Left = 7, Right = 7 }));
            textBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrushKey)));
            Resources.Add(typeof(TextBox), textBoxStyle);

            var radioButtonStyle = new System.Windows.Style(typeof(RadioButton));
            radioButtonStyle.Setters.Add(new Setter(RadioButton.MarginProperty, new Thickness() { Bottom = 7 }));
            radioButtonStyle.Setters.Add(new Setter(RadioButton.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrushKey)));
            Resources.Add(typeof(RadioButton), radioButtonStyle);
        }

        protected void BindToOption(CheckBox checkbox, Option<bool> optionKey)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<bool>(OptionStore, optionKey),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(CheckBox checkbox, Option<int> optionKey)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<int>(OptionStore, optionKey),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default,
                Converter = new CheckBoxCheckedToIntConverter(),
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindTristateToOption(CheckBox checkbox, Option<int> optionKey)
        {
            Debug.Assert(checkbox.IsThreeState);

            var binding = new Binding()
            {
                Source = new OptionBinding<int>(OptionStore, optionKey),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default,
                Converter = new CheckBoxTristateCheckedToIntConverter(),
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(CheckBox checkbox, PerLanguageOption<bool> optionKey, string languageName)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<bool>(OptionStore, optionKey, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(TextBox textBox, Option<int> optionKey)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<int>(OptionStore, optionKey),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default
            };

            var bindingExpression = textBox.SetBinding(TextBox.TextProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(TextBox textBox, PerLanguageOption<int> optionKey, string languageName)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<int>(OptionStore, optionKey, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default
            };

            var bindingExpression = textBox.SetBinding(TextBox.TextProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption<T>(RadioButton radiobutton, PerLanguageOption<T> optionKey, T optionValue, string languageName)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<T>(OptionStore, optionKey, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default,
                Converter = new RadioButtonCheckedConverter(),
                ConverterParameter = optionValue
            };

            var bindingExpression = radiobutton.SetBinding(RadioButton.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToFullSolutionAnalysisOption(CheckBox checkbox, string languageName)
        {
            checkbox.Visibility = Visibility.Visible;

            var binding = new Binding()
            {
                Source = new FullSolutionAnalysisOptionBinding(OptionStore, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        internal virtual void OnLoad()
        {
            foreach (var bindingExpression in _bindingExpressions)
            {
                bindingExpression.UpdateTarget();
            }
        }

        internal virtual void OnSave()
        {
        }

        internal virtual void Close()
        {
        }
    }

    public class RadioButtonCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    public class CheckBoxCheckedToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return !value.Equals(-1);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return value.Equals(true) ? 1 : -1;
        }
    }

    public class CheckBoxTristateCheckedToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value.Equals(0))
            {
                return null;
            }

            return !value.Equals(-1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return 0;
            }

            return value.Equals(true) ? 1 : -1;
        }
    }
}
