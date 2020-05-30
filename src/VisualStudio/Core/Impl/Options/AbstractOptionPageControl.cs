// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            var comboBoxStyle = new System.Windows.Style(typeof(ComboBox));
            comboBoxStyle.Setters.Add(new Setter(ComboBox.MarginProperty, new Thickness() { Bottom = 7 }));
            comboBoxStyle.Setters.Add(new Setter(ComboBox.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrushKey)));
            Resources.Add(typeof(ComboBox), comboBoxStyle);
        }

        private protected void BindToOption(CheckBox checkbox, Option2<bool> optionKey)
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

        private protected void BindToOption(CheckBox checkbox, PerLanguageOption2<bool> optionKey, string languageName)
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

        private protected void BindToOption(TextBox textBox, Option2<int> optionKey)
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

        private protected void BindToOption(TextBox textBox, PerLanguageOption2<int> optionKey, string languageName)
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

        private protected void BindToOption<T>(ComboBox comboBox, Option2<T> optionKey)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<T>(OptionStore, optionKey),
                Path = new PropertyPath("Value"),
                Converter = new ComboBoxItemTagToIndexConverter(),
                ConverterParameter = comboBox
            };

            var bindingExpression = comboBox.SetBinding(ComboBox.SelectedIndexProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        private protected void BindToOption<T>(ComboBox comboBox, PerLanguageOption2<T> optionKey, string languageName)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<T>(OptionStore, optionKey, languageName),
                Path = new PropertyPath("Value"),
                Converter = new ComboBoxItemTagToIndexConverter(),
                ConverterParameter = comboBox
            };

            var bindingExpression = comboBox.SetBinding(ComboBox.SelectedIndexProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        private protected void BindToOption<T>(RadioButton radiobutton, PerLanguageOption2<T> optionKey, T optionValue, string languageName)
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

    public class ComboBoxItemTagToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var comboBox = (ComboBox)parameter;

            for (var index = 0; index < comboBox.Items.Count; index++)
            {
                var item = (ComboBoxItem)comboBox.Items[index];
                if (item.Tag.Equals(value))
                {
                    return index;
                }
            }

            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var index = (int)value;
            if (index == -1)
            {
                return null;
            }

            var comboBox = (ComboBox)parameter;
            var item = (ComboBoxItem)comboBox.Items[index];
            return item.Tag;
        }
    }
}
