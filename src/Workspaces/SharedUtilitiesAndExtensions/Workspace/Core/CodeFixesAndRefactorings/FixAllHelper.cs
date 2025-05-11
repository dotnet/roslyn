// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal static class FixAllHelper
{
    public static string GetDefaultFixAllTitle(
        FixAllScope fixAllScope,
        string title,
        Document triggerDocument,
        Project triggerProject)
    {
        return fixAllScope switch
        {
            FixAllScope.Custom => string.Format(WorkspaceExtensionsResources.Fix_all_0, title),
            FixAllScope.Document => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, title, triggerDocument.Name),
            FixAllScope.Project => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, title, triggerProject.Name),
            FixAllScope.Solution => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Solution, title),
            FixAllScope.ContainingMember => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_member, title),
            FixAllScope.ContainingType => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_type, title),
            _ => throw ExceptionUtilities.UnexpectedValue(fixAllScope),
        };
    }
}
