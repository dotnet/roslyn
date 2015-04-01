using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Extensions
{
    interface IErrorReportingService : IWorkspaceService
    {
        void ShowErrorInfoForCodeFix(string codefixName, Action OnEnableClicked, Action OnEnableAndIgnoreClicked);
    }
}
