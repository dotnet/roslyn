// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal interface IHierarchyItemToProjectIdMap : IWorkspaceService
    {
        bool TryGetProjectId(IVsHierarchyItem hierarchyItem, out ProjectId projectId);
    }
}
