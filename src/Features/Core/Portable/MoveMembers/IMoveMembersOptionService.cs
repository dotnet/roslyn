// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 


using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MoveMembers
{
    internal interface IMoveMembersOptionService : IWorkspaceService
    {
        MoveMembersOptions? GetMoveMembersOptions(Document document, MoveMembersAnalysisResult analysis, MoveMembersEntryPoint entryPoint);
    }
}
