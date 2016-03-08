// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Roslyn.Utilities;

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
            groupBoxStyle.Setters.Add(new Setter(GroupBox.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrushKey)));
            Resources.Add(typeof(CheckBox), checkBoxStyle);
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

        protected void BindToFullSolutionAnalysisOption(CheckBox checkbox, string languageName)
        {
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
}
