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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls
{
    /// <summary>
    /// Interaction logic for MoveToNewTypeControl.xaml
    /// </summary>
    internal partial class MoveToNewTypeControl : UserControl
    {
        public MoveToNewTypeControlViewModel ViewModel { get; }
        public string SelectCurrentFileAsDestination => ServicesVSResources.Add_to_current_file;
        public string SelectNewFileAsDestination => ServicesVSResources.New_file_name_colon;
        public string NewInterfaceName => ServicesVSResources.New_interface_name_colon;

        public MoveToNewTypeControl(MoveToNewTypeControlViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
