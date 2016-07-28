using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal class LazyTip : ContentControl
    {
        private readonly Func<ToolTip> _createToolTip;
        private readonly FrameworkElement _element;

        public LazyTip(FrameworkElement element, Func<ToolTip> createToolTip)
        {
            _element = element;
            _createToolTip = createToolTip;

            element.ToolTipOpening += this.OnToolTipOpening;
            Background = Brushes.Transparent;
        }

        private void OnToolTipOpening(object sender, ToolTipEventArgs e)
        {
            _element.ToolTipOpening -= this.OnToolTipOpening;

            if (this.Content == null)
            {
                _element.ToolTip = _createToolTip();
                return;
                //var content = _createContent();
                //this.Content = content;
                //return;
            }

            _element.ToolTip = null;
            e.Handled = true;
        }
    }
}
