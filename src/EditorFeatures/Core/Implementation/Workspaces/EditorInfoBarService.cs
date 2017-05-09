using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal class EditorInfoBarService : IInfoBarService
    {
        public void ShowInfoBarInActiveView(string message, params InfoBarUI[] items)
        {
            ShowInfoBarInGlobalView(message, items);
        }

        public void ShowInfoBarInGlobalView(string message, params InfoBarUI[] items)
        {
            Logger.Log(FunctionId.Extension_InfoBar, message);
        }
    }
}
