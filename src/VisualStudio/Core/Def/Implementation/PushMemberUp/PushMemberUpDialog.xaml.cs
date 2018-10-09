using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PushMemberUp
{
    /// <summary>
    /// Interaction logic for PushMemberUpDialogxaml.xaml
    /// </summary>
    internal partial class PushMemberUpDialogxaml : DialogWindow
    {
        public string OK => ServicesVSResources.OK;

        public string Cancel => ServicesVSResources.Cancel;

        // TODO: Add this to resources mananger
        public string PushMembersUpTitle => "Push Up Members";

        private PushMemberUpViewModel ViewModel { get; }

        internal PushMemberUpDialogxaml(PushMemberUpViewModel pushMemberUpViewModel)
        {
            ViewModel = pushMemberUpViewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void TargetMembersContainer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedTarget = TargetMembersContainer.SelectedItem as MemberSymbolViewModelGraphNode;

            if (ViewModel.SelectedTarget.MemberSymbolViewModel.MemberSymbol is INamedTypeSymbol nameTypeSymbol && nameTypeSymbol.TypeKind == TypeKind.Interface)
            {
                // TODO
                // If the target is interface, disable and clean the field option
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedMembers = ViewModel.SelectedMembersContainer.
                Where(memberSymbolView => memberSymbolView.IsChecked).
                Select(memberSymbolView => memberSymbolView.MemberSymbol);
            if (ViewModel.SelectedTarget != null && selectedMembers.Count() != 0)
            {
                DialogResult = true;
            }
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void AbstractCheckBox_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
