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
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    /// <summary>
    /// Interaction logic for PushMemberUpDialogxaml.xaml
    /// </summary>
    internal partial class PullMemberUpDialogWarningxaml : DialogWindow
    {
        // TODO: Add these to Service resources
        public string Back => "Back";

        public string Finish => "Finish";

        public string PullMembersUpTitle => "Pull Up Members";

        internal PullMemberUpDialogWarningxaml(PullMemberUpWarningViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void BackButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
