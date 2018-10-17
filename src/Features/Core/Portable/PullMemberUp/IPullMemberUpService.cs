using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal interface IPullMemberUpService : IWorkspaceService
    {
        PullTargetsResult GetPullTargetAndMembers(ISymbol selectedNodeSymbol, IEnumerable<ISymbol> members, Dictionary<ISymbol, Lazy<List<ISymbol>>> lazyDependentsMap);

        bool CreateWarningDialog(List<string> warningMessageList);

        PullTargetsResult RestoreSelectionDialog();
    }
}
