// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal interface IPullMemberUpOptionsService : IWorkspaceService
    {
        PullMembersUpOptions GetPullMemberUpOptions(Document document, ImmutableArray<ISymbol> selectedNodeSymbols);
    }
}
