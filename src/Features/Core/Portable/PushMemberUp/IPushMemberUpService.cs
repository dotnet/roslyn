using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal interface IPushMemberUpService : IWorkspaceService
    {
        PushTargetsResult GetPushTargetAndMembers(INamedTypeSymbol selectedNodeOwnerSymbol, IEnumerable<ISymbol> members);
    }
}
