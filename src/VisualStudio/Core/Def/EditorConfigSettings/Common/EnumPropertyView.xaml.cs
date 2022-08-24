// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Automation;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common
{

    /// <summary>
    /// Interaction logic for EnumPropertyView.xaml
    /// </summary>
    internal partial class EnumSettingView : UserControl
    {
        private readonly IEnumSettingViewModel _model;

        public EnumSettingView(IEnumSettingViewModel model)
        {
            InitializeComponent();
            DataContext = model;
            _model = model;
        }

        private void EnumValueComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = EnumValueComboBox.SelectedIndex;
            var descriptions = _model.EnumValues;
            if (index < descriptions.Length && index >= 0)
            {
                _model.ChangeProperty(descriptions[index]);
            }
        }
    }
}
