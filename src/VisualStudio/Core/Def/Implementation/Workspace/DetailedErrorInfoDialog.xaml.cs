using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class DetailedErrorInfoDialog : DialogWindow
    {
        private readonly string errorInfo;

        internal DetailedErrorInfoDialog(string title, string errorInfo)
        {
            InitializeComponent();
            this.errorInfo = errorInfo;
            this.Title = title;
            stackTraceText.AppendText(errorInfo);
            this.CopyButton.Content = ServicesVSResources.Copy_to_Clipboard;
            this.CloseButton.Content = ServicesVSResources.Close;
        }

        private void CopyMessageToClipBoard(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(errorInfo);
            }
            catch (Exception)
            {
                // rdpclip.exe not running in a TS session, ignore
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
