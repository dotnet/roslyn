// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

[DesignerCategory("code")] // this must be fully qualified
public abstract class AbstractOptionPageControl : UserControl
{
    internal readonly OptionStore OptionStore;

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

    internal virtual void OnLoad()
    {
    }

    internal virtual void OnSave()
    {
    }

    internal virtual void Close()
    {
    }
}
