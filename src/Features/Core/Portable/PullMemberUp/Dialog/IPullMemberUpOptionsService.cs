// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal interface IPullMemberUpOptionsService : IWorkspaceService
    {
        PullMemberDialogResult GetPullTargetAndMembers(SemanticModel semanticModel, ISymbol selectedNodeSymbol, IEnumerable<ISymbol> members);
    }
}
