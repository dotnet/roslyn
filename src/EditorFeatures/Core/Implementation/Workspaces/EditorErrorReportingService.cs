using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal class EditorErrorReportingService : IErrorReportingService
    {
        public void ShowErrorInfoForCodeFix(string codefixName, Action OnEnableClicked, Action OnEnableAndIgnoreClicked)
        {
            var message = LogMessage.Create($"{codefixName} crashed");
            Logger.Log(FunctionId.Extension_Exception, message);
        }
    }
}
