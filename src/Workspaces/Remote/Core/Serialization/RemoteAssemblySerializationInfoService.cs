// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote.Serialization
{
    [ExportWorkspaceService(typeof(IAssemblySerializationInfoService), SolutionService.WorkspaceKind_RemoteWorkspace), Shared]
    internal class RemoteAssemblySerializationInfoService : AbstractAssemblySerializationInfoService
    {
    }
}