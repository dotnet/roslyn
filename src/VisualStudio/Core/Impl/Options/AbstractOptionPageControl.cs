// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [System.ComponentModel.DesignerCategory("code")] // this must be fully qualified
    public abstract class AbstractOptionPageControl : UserControl
    {
        internal readonly IOptionService OptionService;
        private readonly List<BindingExpressionBase> _bindingExpressions = new List<BindingExpressionBase>();

        public AbstractOptionPageControl(IServiceProvider serviceProvider)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            this.OptionService = workspace.Services.GetService<IOptionService>();

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

        protected void AddBinding(BindingExpressionBase bindingExpression)
        {
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(CheckBox checkbox, Option<bool> optionKey)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<bool>(OptionService, optionKey),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(CheckBox checkbox, PerLanguageOption<bool> optionKey, string languageName)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<bool>(OptionService, optionKey, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(TextBox textBox, Option<int> optionKey)
        {
            var binding = new Binding()
            {
                Source = new OptionBinding<int>(OptionService, optionKey),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };

            var bindingExpression = textBox.SetBinding(TextBox.TextProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption(TextBox textBox, PerLanguageOption<int> optionKey, string languageName)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<int>(OptionService, optionKey, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };

            var bindingExpression = textBox.SetBinding(TextBox.TextProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToOption<T>(RadioButton radiobutton, PerLanguageOption<T> optionKey, T optionValue, string languageName)
        {
            var binding = new Binding()
            {
                Source = new PerLanguageOptionBinding<T>(OptionService, optionKey, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
                Converter = new RadioButtonCheckedConverter(),
                ConverterParameter = optionValue
            };

            var bindingExpression = radiobutton.SetBinding(RadioButton.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        protected void BindToFullSolutionAnalysisOption(CheckBox checkbox, string languageName)
        {
            // Full solution analysis option has been moved to error list from Dev14 Update3.
            // We only want to show the full solution analysis option in Tools Options, if we are running against prior VS bits.
            if (VisualStudioDiagnosticListTable.ErrorListHasFullSolutionAnalysisButton())
            {
                checkbox.Visibility = Visibility.Collapsed;
                return;
            }

            checkbox.Visibility = Visibility.Visible;
                        
            Binding binding = new Binding()
            {
                Source = new FullSolutionAnalysisOptionBinding(OptionService, languageName),
                Path = new PropertyPath("Value"),
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };

            var bindingExpression = checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
            _bindingExpressions.Add(bindingExpression);
        }

        internal virtual void LoadSettings()
        {
            foreach (var bindingExpression in _bindingExpressions)
            {
                bindingExpression.UpdateTarget();
            }
        }

        internal virtual void SaveSettings()
        {
            foreach (var bindingExpression in _bindingExpressions)
            {
                if (!bindingExpression.IsDirty)
                {
                    continue;
                }

                bindingExpression.UpdateSource();
            }
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
}
