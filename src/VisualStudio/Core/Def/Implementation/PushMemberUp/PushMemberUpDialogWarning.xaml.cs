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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PushMemberUp
{
    /// <summary>
    /// Interaction logic for PushMemberUpDialogxaml.xaml
    /// </summary>
    internal partial class PushMemberUpDialogWarningxaml : DialogWindow
    {
        internal PushMemberUpDialogWarningxaml(PushMemberUpViewModel pushMemberUpViewModel)
        {
            InitializeComponent();
        }
    }
}
