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
        private readonly string[] _descriptions;
        private readonly ComboBox _comboBox;

        public EnumSettingView(IEnumSettingViewModel model)
        {
            InitializeComponent();
            _model = model;

            _descriptions = _model.GetValueDescriptions();
            _comboBox = new ComboBox()
            {
                ItemsSource = _descriptions
            };

            _comboBox.SelectedIndex = model.GetValueIndex();
            _comboBox.SetValue(AutomationProperties.NameProperty, ServicesVSResources.Value);
            _comboBox.SelectionChanged += ComboBox_SelectionChanged;

            _ = RootGrid.Children.Add(_comboBox);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = _comboBox.SelectedIndex;
            if (index < _descriptions.Length && index >= 0)
            {
                _model.ChangeProperty(_descriptions[index]);
            }
        }
    }
}
