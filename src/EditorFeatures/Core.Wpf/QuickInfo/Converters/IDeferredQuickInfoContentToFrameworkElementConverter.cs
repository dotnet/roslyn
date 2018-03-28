using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    interface IDeferredQuickInfoContentToFrameworkElementConverter
    {
        Type GetApplicableType();
        FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory);
    }
}
