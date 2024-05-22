// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Converters;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [DesignerCategory("code")] // this must be fully qualified
    public abstract class AbstractOptionPageControl : UserControl
    {
        internal readonly OptionStore OptionStore;
        private readonly List<BindingExpressionBase> _bindingExpressions = [];
        private readonly List<OptionPageSearchHandler> _searchHandlers = [];

        private protected AbstractOptionPageControl(OptionStore optionStore)
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

            AddSearchHandler(checkbox);

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        private protected void BindToOption(CheckBox checkbox, Option2<bool?> nullableOptionKey, Func<bool> onNullValue)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<bool?>(OptionStore, nullableOptionKey),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default,
                Converter = new NullableBoolOptionConverter(onNullValue)
            };

            AddSearchHandler(checkbox);

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

            AddSearchHandler(checkbox);

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        private protected void BindToOption(CheckBox checkbox, PerLanguageOption2<bool?> nullableOptionKey, string languageName, Func<bool> onNullValue)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<bool?>(OptionStore, nullableOptionKey, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Default,
                Converter = new NullableBoolOptionConverter(onNullValue)
            };

            AddSearchHandler(checkbox);

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

        private protected void BindToOption<T>(ComboBox comboBox, Option2<T> optionKey, ContentControl label = null)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<T>(OptionStore, optionKey),
                Path = new PropertyPath("Value"),
                Converter = new ComboBoxItemTagToIndexConverter(),
                ConverterParameter = comboBox
            };

            AddSearchHandler(comboBox);

            if (label is not null)
                AddSearchHandler(label);

            var bindingExpression = comboBox.SetBinding(ComboBox.SelectedIndexProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        private protected void BindToOption<T>(ComboBox comboBox, PerLanguageOption2<T> optionKey, string languageName, ContentControl label = null)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<T>(OptionStore, optionKey, languageName),
                Path = new PropertyPath("Value"),
                Converter = new ComboBoxItemTagToIndexConverter(),
                ConverterParameter = comboBox
            };

            AddSearchHandler(comboBox);

            if (label is not null)
                AddSearchHandler(label);

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

            AddSearchHandler(radiobutton);

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

        internal virtual void OnSearch(string searchString)
        {
            var shouldScrollIntoView = true;
            foreach (var handler in _searchHandlers)
            {
                if (handler.TryHighlightSearchString(searchString) && shouldScrollIntoView)
                {
                    handler.EnsureVisible();
                    shouldScrollIntoView = false;
                }
            }
        }

        private protected void AddSearchHandler(ComboBox comboBox)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                AddSearchHandler(item);
            }
        }

        private protected void AddSearchHandler(ContentControl control)
        {
            Debug.Assert(control.Content is string, $"I don't know how to add keyword search support for the '{control.GetType().Name}' control with content type '{control.Content?.GetType().Name ?? "null"}'");
            if (control.Content is string content)
            {
                _searchHandlers.Add(new OptionPageSearchHandler(control, content));
            }
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
